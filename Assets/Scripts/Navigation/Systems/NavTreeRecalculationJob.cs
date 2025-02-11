using Grids;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Navigation
{
    [WithAll(typeof(NavTree), typeof(RoomModel))]
    public partial struct NavTreeRecalculationJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction] 
        public ComponentLookup<NavTree> treeLookup;
        public uint currentTick;
        void Execute(Entity e, in CGrid grid)
        {
            var tree = treeLookup[e];
            if(!tree.IsDirty)
                return;
            var sameRoomMap = new NativeHashSet<(int, int)>(32, Allocator.Temp);
            var roomRemapMap = new NativeHashMap<int, int>(32, Allocator.Temp);
            
            var bbox = grid.BoundsWithBorder;
            tree.Init();
            tree.LastUpdate = currentTick;
            var maxArea = 0;
            for (int x = bbox.Min.x; x <= bbox.Max.x; x++)
            for (int y = bbox.Min.y; y <= bbox.Max.y; y++)
            {
                var selfCell = new int2(x, y);
                var pathing = grid.GetPathing(selfCell);
                CellType cellType;
                if(pathing == default)
                    cellType = CellType.Space;
                else if(pathing.IsFloor() && !pathing.IsWall())
                    cellType = CellType.Floored;
                else if(pathing.IsFloor() && (pathing & BuildingLayer.Door) == BuildingLayer.Door)
                    cellType = CellType.Portal;
                else
                    continue;
                
                var cell = new NavCell { Type = cellType, Area = ++maxArea };

                foreach (var nb in Neighbours)
                {
                    var nbCell = selfCell + nb;
                    if(!tree.Cells.TryGetValue(nbCell, out var otherCell) || otherCell.Type != cellType)
                        continue;

                    var selfArea = cell.Area;
                    var nbArea = otherCell.Area;
                    var sharedArea = math.min(selfArea, nbArea);
                    if(selfArea > sharedArea)
                        sameRoomMap.Add((selfArea, sharedArea));
                    if(nbArea > sharedArea)
                        sameRoomMap.Add((nbArea, sharedArea));
                        
                    otherCell.Area = sharedArea;
                    tree.Cells[nbCell] = otherCell;
                    cell.Area = sharedArea;
                        
                }

                tree.Cells[selfCell] = cell; 
            }
            
            var queue = new NativeQueue<(int, int)>(Allocator.Temp);
            foreach (var value in sameRoomMap)
                queue.Enqueue(value);
            
            // Combine neighbouring rooms
            while(queue.TryDequeue(out var remap))
            {
                var newRoom = math.min(remap.Item1, remap.Item2);
                if (roomRemapMap.TryGetValue(remap.Item1, out var other))
                    newRoom = math.min(newRoom, other);
                if (roomRemapMap.TryGetValue(remap.Item2, out var other2))
                    newRoom = math.min(newRoom, other2);
                roomRemapMap[remap.Item1] = newRoom;
                roomRemapMap[remap.Item2] = newRoom;
                if(other != remap.Item1 && other != remap.Item2 && other != newRoom && other != 0)
                    queue.Enqueue((other, newRoom));
                if(other2 != remap.Item1 && other2 != remap.Item2 && other2 != newRoom && other2 != 0 && other2 != other)
                    queue.Enqueue((other2, newRoom));
            }
            
            // Calculate final rooms
            foreach (var cell in tree.Cells.GetKeyArray(Allocator.Temp))
            {
                var roomId = tree.Cells[cell].Area;
                if (roomId == 0)
                {
                    Debug.LogError($"Something wrong! {cell} has room {roomId}");
                    continue;
                }
                while(roomRemapMap.TryGetValue(roomId, out var remap) && roomId != remap)
                    roomId = remap;
                var c = tree.Cells[cell];
                c.Area = roomId;
                tree.Cells[cell] = c;
                
                if(!tree.Areas.TryGetValue(roomId, out var area))
                    area = new AreaData(Allocator.Persistent) { Type = tree.Cells[cell].Type };
                area.Cells.Add(cell);
                var bounds = area.AABB;
                bounds.Encapsulate(cell);
                area.AABB = bounds;
                tree.Areas[roomId] = area;
            }
            
            // Remap adjacent areas
            foreach (var areaId in tree.Areas.GetKeyArray(Allocator.Temp))
            {
                var area = tree.Areas[areaId];
                var connections = area.Connections;
                foreach(var cell in area.Cells)
                    foreach(var nb in Neighbours)
                        if (tree.Cells.TryGetValue(cell + nb, out var neighbour) && neighbour.Area != areaId)
                        {
                            area.AdjacentCellsByArea.Add(neighbour.Area, cell + nb);
                            connections[neighbour.Area] = new AreaNavigationData
                            {
                                PreviousArea = areaId,
                                PreviousAreaTraversalCost = 1
                            };
                        }
                area.Connections = connections;
                tree.Areas[areaId] = area;
            }
            
            CalculateAreaConnections(ref tree);
            
            treeLookup[e] = tree;
        }

        internal void CalculateAreaConnections(ref NavTree tree)
        {
            var changed = true;
            while (changed)
            {
                changed = false;

                foreach (var areaId in tree.Areas.GetKeyArray(Allocator.Temp))
                {
                    var area = tree.Areas[areaId];

                    foreach (var neighborAreaId in area.Connections.GetKeyArray(Allocator.Temp))
                    {
                        var navData = area.Connections[neighborAreaId];
                        if (!tree.Areas.TryGetValue(neighborAreaId, out var neighborArea))
                            continue;

                        foreach (var otherAreaId in neighborArea.Connections.GetKeyArray(Allocator.Temp))
                        {
                            if (otherAreaId == areaId) continue; // Avoid looping back
                            var otherNavData = neighborArea.Connections[otherAreaId];
                            var newCost = navData.PreviousAreaTraversalCost + otherNavData.PreviousAreaTraversalCost;

                            if (area.Connections.ContainsKey(otherAreaId) &&
                                newCost >= area.Connections[otherAreaId].PreviousAreaTraversalCost) 
                                continue;
                            var connections = area.Connections;
                            connections[otherAreaId] = new AreaNavigationData
                            {
                                PreviousArea = neighborAreaId,
                                PreviousAreaTraversalCost = newCost
                            };
                            area.Connections = connections;
                            changed = true;
                        }
                    }

                    tree.Areas[areaId] = area;
                }
            }
        }
    
        static readonly int2[] Neighbours =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };
    }
}