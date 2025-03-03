using Grids;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Navigation
{
    [BurstCompile]
    [WithAll(typeof(GridLayoutChangedEvent))]
    public partial struct NavTreeRecalculationJob : IJobEntity
    {
        public uint currentTick;
        
        [BurstCompile]
        void Execute(Entity e, in CGrid grid, ref NavTree tree)
        {
            var sameRoomMap = new NativeHashSet<int2>(32, Allocator.Temp);
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
                var cellType = CellType.Blocked;
                if(pathing.IsFloor())
                    cellType = CellType.Floored;
                else
                    cellType = CellType.Space;
                if((pathing & BuildingLayer.Door) == BuildingLayer.Door)
                    cellType |= CellType.Portal;
                else if(pathing.IsWall())
                    cellType = CellType.Blocked;
                
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
                        sameRoomMap.Add(new int2(selfArea, sharedArea));
                    if(nbArea > sharedArea)
                        sameRoomMap.Add(new int2(nbArea, sharedArea));
                        
                    otherCell.Area = sharedArea;
                    tree.Cells[nbCell] = otherCell;
                    cell.Area = sharedArea;
                        
                }

                tree.Cells[selfCell] = cell; 
            }
            
            var queue = new NativeQueue<int2>(Allocator.Temp);
            foreach (var value in sameRoomMap)
                queue.Enqueue(value);
            
            // Combine neighbouring rooms
            while(queue.TryDequeue(out var remap))
            {
                var newRoom = math.min(remap.x, remap.y);
                if (roomRemapMap.TryGetValue(remap.x, out var other))
                    newRoom = math.min(newRoom, other);
                if (roomRemapMap.TryGetValue(remap.y, out var other2))
                    newRoom = math.min(newRoom, other2);
                roomRemapMap[remap.x] = newRoom;
                roomRemapMap[remap.y] = newRoom;
                if(other != remap.x && other != remap.y && other != newRoom && other != 0)
                    queue.Enqueue(new int2(other, newRoom));
                if(other2 != remap.x && other2 != remap.y && other2 != newRoom && other2 != 0 && other2 != other)
                    queue.Enqueue(new int2(other2, newRoom));
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
                if(area.Type == CellType.Space && area.AABB.Size == grid.BoundsWithBorder.Size)
                    area.Type = CellType.Outside;
                tree.Areas[roomId] = area;
            }
            
            NavTree.RecalculateAreaConnections(ref tree);
        }

        static readonly int2[] Neighbours =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };
    }
}