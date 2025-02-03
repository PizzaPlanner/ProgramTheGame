using Grids;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

[WithAll(typeof(RoomModel))]
public partial struct GridRoomRecalculationJob : IJobEntity
{
    void Execute(Entity e, in CGrid grid, ref RoomModel roomModel)
    {
        if(roomModel.WallChanges.Length == 0)
            return;
        
        var maxRoom = roomModel.MaxRoom;
        var bbox = grid.BoundsWithBorder;
        var toCheck = new NativeQueue<int2>(Allocator.Temp);
        var checkedCells = new NativeHashMap<int2, int>(0, Allocator.Temp);
        var sameRoomMap = new NativeHashSet<(int, int)>(0, Allocator.Temp);
        var roomRemapMap = new NativeHashMap<int, int>(0, Allocator.Temp);
        
        // Flood-fill grid, starting with all neighbours or walls
        foreach(var wall in roomModel.Walls)
            checkedCells.Add(wall, -1);

        foreach (var change in roomModel.WallChanges)
        {
            foreach (var neighbour in Neighbours)
                toCheck.Enqueue(change + neighbour);
            roomModel.CellRoomMap.Remove(change);
        }

        // Assign cell room values depending on neighbours
        while (toCheck.TryDequeue(out var cell))
        {
            if(checkedCells.ContainsKey(cell))
                continue;
            
            var lastNeighbour = 0;
            foreach (var neighbour in Neighbours)
            {
                var otherCell = cell + neighbour;
                var value = -1;
                if (!bbox.Contains(otherCell))
                    value = -2;
                else if (!checkedCells.TryGetValue(otherCell, out value))
                {
                    toCheck.Enqueue(otherCell);
                    continue;
                }

                if (value == -1) continue;
                // If cell has multiple neighbours with different rooms,
                // Mark rooms as the same
                if(lastNeighbour != value && lastNeighbour != 0)
                    sameRoomMap.Add((lastNeighbour, value));
                lastNeighbour = value;
            }

            if(lastNeighbour == 0)
                lastNeighbour = ++maxRoom;
            
            checkedCells.Add(cell, lastNeighbour);
        }

        var queue = new NativeQueue<(int, int)>(Allocator.Temp);
        foreach(var value in sameRoomMap)
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
            if(other != remap.Item1 && other != remap.Item2 && other != newRoom)
                queue.Enqueue((other, newRoom));
            if(other2 != remap.Item1 && other2 != remap.Item2 && other2 != other && other2 != newRoom)
                queue.Enqueue((other2, newRoom));
        }
        
        // Calculate final room AABBs
        var finalRoomMap = new NativeHashMap<int, Room>(0, Allocator.Temp);
        foreach (var cell in checkedCells.GetKeyArray(Allocator.Temp))
        {
            var roomId = checkedCells[cell];
            while(roomRemapMap.TryGetValue(roomId, out var remap) && roomId != remap)
                roomId = remap;
            if (roomId < 0)
            {
                roomModel.CellRoomMap.Remove(cell);
                continue;
            }
            roomModel.CellRoomMap[cell] = roomId;
            var aabb = new GridBounds() { Max = cell, Min = cell };
            if (finalRoomMap.TryGetValue(roomId, out var existingRoom))
                aabb = new GridBounds()
                {
                    Max = math.max(aabb.Max, existingRoom.BoundingBox.Max),
                    Min = math.min(aabb.Min, existingRoom.BoundingBox.Min)
                };
            finalRoomMap[roomId] = new Room { BoundingBox = aabb };
        }

        // if (finalRoomMap.Count > 0)
        // {
        //     foreach(var room in finalRoomMap)
        //         Debug.Log($"{room.Key} = {room.Value.BoundingBox}");
        //     Debug.Log($"With these remaps:");
        //     foreach(var remap in roomRemapMap.GetKeyArray(Allocator.Temp))
        //         Debug.Log($"{remap} --> {roomRemapMap[remap]}");
        // }
        roomModel.WallChanges.Clear();
        roomModel.MaxRoom = maxRoom;
    }
    
    private static readonly int2[] Neighbours =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };
}