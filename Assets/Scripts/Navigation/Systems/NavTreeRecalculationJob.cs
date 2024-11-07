using System;
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
        [NativeDisableContainerSafetyRestriction, ReadOnly] 
        public ComponentLookup<RoomModel> roomLookup;
        void Execute(Entity e, in GridStructure grid)
        {
            var tree = treeLookup[e];
            if(!tree.IsDirty)
                return;
            var rooms = roomLookup[e];
            var sameRoomMap = new NativeHashSet<(int, int)>(32, Allocator.Temp);
            var roomRemapMap = new NativeHashMap<int, int>(32, Allocator.Temp);
            
            var bbox = grid.LocalAABB;
            tree.Init(bbox);
            var maxArea = 0;
            for (int x = bbox.Min.x; x <= bbox.Max.x; x++)
            for (int y = bbox.Min.y; y <= bbox.Max.y; y++)
            {
                var selfCell = new int2(x, y);
                if(!rooms.IsPathable(selfCell))
                    continue;
                var areas = new FixedList64Bytes<int>
                {
                    Length = tree.MaxLevel
                };
                var cell = new NavCell
                {
                    Areas = areas
                };

                for (int level = 1; level <= tree.MaxLevel; level++)
                {
                    foreach (var nb in Neighbours)
                    {
                        var nbCell = selfCell + nb;
                        if(!tree.Cells.TryGetValue(nbCell, out var otherCell))
                            continue;
                        if(math.any(tree.GetBoundsAtLevel(selfCell, level).Min != tree.GetBoundsAtLevel(nbCell, level).Min))
                            continue;

                        var selfArea = cell.Areas[level - 1];
                        var nbArea = otherCell.Areas[level - 1];
                        var sharedArea = math.min(selfArea, nbArea);
                        if(sharedArea <= NavTree.LEVEL_LIMIT)
                            sharedArea = ++maxArea * NavTree.LEVEL_LIMIT + level;
                        if(selfArea > NavTree.LEVEL_LIMIT)
                            sameRoomMap.Add((selfArea, sharedArea));
                        if(nbArea > NavTree.LEVEL_LIMIT)
                            sameRoomMap.Add((nbArea, sharedArea));
                        
                        otherCell.Areas[level - 1] = sharedArea;
                        tree.Cells[nbCell] = otherCell;
                        cell.Areas[level - 1] = sharedArea;
                        
                    }
                    if (cell.Areas[level - 1] <= NavTree.LEVEL_LIMIT)
                    {
                        cell.Areas[level - 1] = ++maxArea * NavTree.LEVEL_LIMIT + level;
                    }
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
                var areas =  tree.Cells[cell].Areas;
                for (int i = 0; i < tree.MaxLevel; i++)
                {
                    var roomId = areas[i];
                    if(roomId == 0)
                        continue;
                    while(roomRemapMap.TryGetValue(roomId, out var remap) && roomId != remap)
                        roomId = remap;
                    areas[i] = roomId;
                    tree.CellsByArea.Add(areas[i], cell);
                    var bounds  = tree.BoundsByArea.TryGetValue(areas[i], out var b) ? b : GridBounds.Empty;
                    bounds.Encapsulate(cell);
                    tree.BoundsByArea[areas[i]] = bounds;
                }
                var c = tree.Cells[cell];
                c.Areas = areas;
                tree.Cells[cell] = c;
            }
            
            CalculateAreaConnections(ref tree);
            
            treeLookup[e] = tree;
        }
        
        internal void CalculateAreaConnections(ref NavTree tree)
        {
            using var connections = new NativeHashSet<int>(8, Allocator.Temp);
            foreach (var kvp in tree.Cells)
            {
                for (int i = 0; i < tree.MaxLevel; i++)
                {
                    connections.Clear();
                    var area = kvp.Value.Areas[i];
                    foreach(var offset in Neighbours)
                        if(tree.Cells.TryGetValue(kvp.Key + offset, out var otherCell) 
                           && otherCell.Areas[i] != area)
                            connections.Add(otherCell.Areas[i]);
                    
                    if(i > 0)
                        connections.Add(kvp.Value.Areas[i - 1]);
                    if(i < tree.MaxLevel - 1)
                        connections.Add(kvp.Value.Areas[i + 1]);
                    if(!tree.AreaConnections.ContainsKey(area))
                        tree.AreaConnections.Add(area, new UnsafeHashSet<int>(connections.Count, Allocator.Persistent));
                    // Need to use local variable here! (to keep Count correctly?)
                    var hs = tree.AreaConnections[area];
                    foreach (var neighbour in connections)
                        hs.Add(neighbour);
                    tree.AreaConnections[area] = hs;
                }
            }
        }
    
        static readonly int2[] Neighbours =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };
    }
}