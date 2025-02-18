using Grids;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[assembly:InternalsVisibleTo("Navigation.Systems")]

namespace Navigation
{
    [Flags]
    public enum CellType 
    {
        Blocked = 0,
        Space = 1,
        Floored = 2,
        Portal = 4,
        Outside = 8 | Space,
        Walkable = Floored | Portal,
        Flyable = Walkable | Space
    }
    
    public struct NavCell
    {
        public int Area;
        public CellType Type;
    }

    public struct AreaNavigationData
    {
        public int PreviousArea;
        public int PreviousAreaTraversalCost;
    }

    public struct AreaData : IDisposable
    {
        public UnsafeHashSet<int2> Cells;
        // Cells directly adjacent to this area that are located in other areas.
        public UnsafeParallelMultiHashMap<int, int2> AdjacentCellsByArea;
        public GridBounds AABB;
        public UnsafeHashMap<int, AreaNavigationData> Connections;
        public CellType Type;

        public AreaData(Allocator allocator)
        {
            Cells = new (16, allocator);
            AdjacentCellsByArea = new (16, allocator);
            Connections = new (16, allocator);
            AABB = GridBounds.Empty;
            Type = CellType.Blocked;
        }
        
        public void Dispose()
        {
            Cells.Dispose();
            Connections.Dispose();
            AdjacentCellsByArea.Dispose();
        }
    }
    
    public struct NavTree : ICleanupComponentData, IDisposable
    {
        // TODO: ChangeAreas data not used
        internal UnsafeList<GridBounds> ChangeAreas;
        internal UnsafeHashMap<int2, NavCell> Cells;
        internal UnsafeParallelHashMap<int, AreaData> Areas;
        public bool IsDirty => ChangeAreas.Length > 0 || Cells.IsEmpty;

        public uint LastUpdate;

        public CellType GetCellType(int2 cell) => Cells.TryGetValue(cell, out var c) ? c.Type : CellType.Blocked;

        // For finding paths to walls, which are not on the navtree
        public bool NavigateAdjacent(int2 start, int2 blockedEnd, CellType allowedTypes, ref NativeList<int2> path)
        {
            Span<int2> neighbours = stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)};
            foreach (var nb in neighbours)
            {
                var cell = blockedEnd + nb;
                if(Cells.TryGetValue(cell, out _))
                    return Navigate(start, cell, allowedTypes, ref path);
            }
            return false;
        }

        public void SetAreaDirty(GridBounds bounds)
        {
            ChangeAreas.Add(bounds);
        }
        
        public void SetAreaDirty(NativeArray<int2> cells)
        {
            foreach (var cell in cells)
                ChangeAreas.Add(new GridBounds{Min = cell, Max = cell});
        }

        public void GetAllConnectedCells(int2 fromCell, ref NativeList<int2> allCells)
        {
            var checkedAreas = new NativeHashSet<int>(4, Allocator.Temp);
            GetAllConnectedCells(fromCell, ref allCells, ref checkedAreas);
        }
        
        void GetAllConnectedCells(int2 fromCell, ref NativeList<int2> allCells, ref NativeHashSet<int> checkedAreas)
        {
            if(!Cells.TryGetValue(fromCell, out var c))
                return;
            if(!checkedAreas.Add(c.Area))
                return;
            if(!Areas.TryGetValue(c.Area, out var area))
                return;
            if(area.Type == CellType.Outside)
                return;
            
            allCells.AddRange(area.Cells.ToNativeArray(Allocator.Temp));
            foreach(var nbArea in area.AdjacentCellsByArea.GetKeyArray(Allocator.Temp))
                if(area.AdjacentCellsByArea.TryGetFirstValue(nbArea, out var nb, out _))
                    GetAllConnectedCells(nb, ref allCells, ref checkedAreas);    
        }

        public bool IsReachable(int2 start, int2 end, CellType allowedTypes, bool allowAdjacent)
        {
            if (allowAdjacent)
            {
                foreach(var nb in stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)})
                    if(IsReachable(start, end + nb, allowedTypes, false))
                        return true;
            }
            if(!Cells.TryGetValue(end, out var toCell) || (toCell.Type & allowedTypes) == CellType.Blocked)
                return false;
            if(!Cells.TryGetValue(start, out var fromCell) || (fromCell.Type & allowedTypes) == CellType.Blocked)
                return false;
            return fromCell.Area == toCell.Area || Areas[fromCell.Area].Connections.ContainsKey(toCell.Area);
        }
        
        public bool Navigate(int2 start, int2 end, CellType allowedTypes, ref NativeList<int2> path)
        {
            if(!IsReachable(start, end, allowedTypes, false))
                return false;
            var fromCell = Cells[start];
            var toCell = Cells[end];
                AStarDirect(start, end, allowedTypes, ref path);
            // if(fromCell.Area == toCell.Area)
            //     AStarDirect(start, end, allowedTypes, ref path);
            // else
            // {
            //     // Find path within fromCell.Area towards the next area closest on path to toCell.Area
            //     // This also needs to include the first cell traversed through in nextArea, so we can run again from there when reached
            //     var nextArea = toCell.Area;
            //     while(Areas[fromCell.Area].Connections[nextArea].PreviousAreaTraversalCost > 1)
            //         nextArea = Areas[fromCell.Area].Connections[nextArea].PreviousArea;
            //     AStarToArea(start, nextArea, ref path);
            // }
            path.Add(end);
            return true;
        }

        unsafe void AStarDirect(int2 start, int2 end, CellType allowedTypes, ref NativeList<int2> path)
        {
            var open = new NativeList<int2>(32, Allocator.Temp);
            var closed = new NativeHashSet<int2>(32, Allocator.Temp);
            var bestSourceNodeMap = new NativeHashMap<int2, int2>(32, Allocator.Temp);
            var gScores = new NativeHashMap<int2, int>(32, Allocator.Temp);
            gScores[start] = 0;
            var fScores = new NativeHashMap<int2, int>(32, Allocator.Temp);
            fScores[start] = 0;
            open.Add(start);
            Span<int2> neighbours = stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)};
            int2 current = 0;
            while (!open.IsEmpty)
            {
                current = open[0];
                open.RemoveAt(0);
                if(math.all(current == end))
                    break;
                if(!closed.Add(current))
                    continue;
                foreach (var neighbour in neighbours)
                {
                    var next = current + neighbour;
                    if(closed.Contains(next) || (GetCellType(next) & allowedTypes) == CellType.Blocked)
                        continue;
                    var gScore = gScores[current] + 1;
                    var previousGScore = gScores.TryGetValue(next, out var s) ? s : int.MaxValue; 
                    if (gScore < previousGScore)
                    {
                        var h = (int)math.distance(CGrid.CellCenterToLocal(end), CGrid.CellCenterToLocal(next));
                        bestSourceNodeMap[next] = current;
                        gScores[next] = gScore;
                        fScores[next] = gScore + h;
                        open.Add(next);
                    }
                }
                open.Sort(new WeightSorter2(fScores));
            }
            
            var fromCell = current;
            while (math.any(fromCell != start))
            {
                path.Add(fromCell);
                fromCell = bestSourceNodeMap[fromCell];
            }
            path.Add(start);
            path.AsArray().AsSpan().Reverse();
            // var p = "";
            // foreach(var a in path)
            //     p += $"{a} -> ";
            // Debug.Log(p[..^4]);
        }
        
        // unsafe void AStarToArea(int2 start, int nextArea, ref NativeList<int2> path)
        // {
        //     // AStar to the best cell from targetArea directly reachable by startArea
        //     var currentArea = 
        //
        //     var open = new NativeList<int2>(32, Allocator.Temp);
        //     var closed = new NativeHashSet<int2>(32, Allocator.Temp);
        //     var bestSourceNodeMap = new NativeHashMap<int2, int2>(32, Allocator.Temp);
        //     var gScores = new NativeHashMap<int2, int>(32, Allocator.Temp);
        //     gScores[start] = 0;
        //     var fScores = new NativeHashMap<int2, int>(32, Allocator.Temp);
        //     fScores[start] = 0;
        //     open.Add(start);
        //     Span<int2> neighbours = stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)};
        //     int2 current = 0;
        //     while (!open.IsEmpty)
        //     {
        //         current = open[0];
        //         open.RemoveAt(0);
        //         var currentCell = Cells[current];
        //         if(currentCell.Areas[targetLevelIndex] == nextArea)
        //             break;
        //         if(currentCell.Areas[targetLevelIndex] != currentArea)
        //             continue;
        //         if(!closed.Add(current))
        //             continue;
        //         foreach (var neighbour in neighbours)
        //         {
        //             var next = current + neighbour;
        //             if(closed.Contains(next) || !Cells.ContainsKey(next))
        //                 continue;
        //             var gScore = gScores[current] + 1;
        //             var previousGScore = gScores.TryGetValue(next, out var s) ? s : int.MaxValue; 
        //             if (gScore < previousGScore)
        //             {
        //                 var h = (int)math.distance(Areas[nextArea].AABB.Center, CGrid.CellCenterToLocal(next));
        //                 bestSourceNodeMap[next] = current;
        //                 gScores[next] = gScore;
        //                 fScores[next] = gScore + h;
        //                 open.Add(next);
        //             }
        //         }
        //         open.Sort(new WeightSorter2(fScores));
        //     }
        //     
        //     var fromCell = current;
        //     while (math.any(fromCell != start))
        //     {
        //         path.Add(fromCell);
        //         fromCell = bestSourceNodeMap[fromCell];
        //     }
        //     path.Add(start);
        //     path.AsArray().AsSpan().Reverse();
        //     // var p = "";
        //     // foreach(var a in path)
        //     //     p += $"{a} -> ";
        //     // Debug.Log(p[..^4]);
        // }
        //
        // NativeList<int> AStar(NavCell start, NavCell end, CellType allowedTypes, int areaLevel)
        // {
        //     var startArea = start.Area;
        //     var endArea = end.Area;
        //     var open = new NativeList<int>(32, Allocator.Temp);
        //     var closed = new NativeHashSet<int>(32, Allocator.Temp);
        //     var bestSourceNodeMap = new NativeHashMap<int, int>(32, Allocator.Temp);
        //     var gScores = new NativeHashMap<int, int>(32, Allocator.Temp);
        //     gScores[startArea] = 0;
        //     var fScores = new NativeHashMap<int, int>(32, Allocator.Temp);
        //     fScores[startArea] = 0;
        //     open.Add(startArea);
        //     while (!open.IsEmpty)
        //     {
        //         var current = open[0];
        //         open.RemoveAt(0);
        //         if(current == endArea)
        //             break;
        //         if(!closed.Add(current))
        //             continue;
        //         foreach (var connection in Areas[current].Connections)
        //         {
        //             var toArea = connection.Key;
        //             if(closed.Contains(toArea)
        //                || (Areas[toArea].Type & allowedTypes) == CellType.Blocked)
        //                 continue;
        //             var gScore = gScores[current] + connection.Value.PreviousAreaTraversalCost;
        //             var previousGScore = gScores.TryGetValue(toArea, out var s) ? s : int.MaxValue; 
        //             if (gScore < previousGScore)
        //             {
        //                 var h = (int)math.distance(Areas[toArea].AABB.Center, Areas[endArea].AABB.Center);
        //                 bestSourceNodeMap[connection] = current;
        //                 gScores[connection] = gScore;
        //                 fScores[connection] = gScore + h;
        //                 open.Add(connection);
        //             }
        //         }
        //         open.Sort(new WeightSorter(fScores));
        //     }
        //
        //     if (bestSourceNodeMap.ContainsKey(endArea))
        //     {
        //         var areas = new NativeList<int>(Allocator.Temp);
        //         var fromArea = endArea;
        //         while (fromArea != startArea)
        //         {
        //             areas.Add(fromArea);
        //             fromArea = bestSourceNodeMap[fromArea];
        //         }
        //         areas.AsArray().AsSpan().Reverse();
        //         // var p = $"{startArea} -> ";
        //         // foreach(var a in areas)
        //         //     p += $"{a} -> ";
        //         // Debug.Log(p[..^4]);
        //         return areas;
        //     }
        //     return default;
        // }
        
        // public int GetNextArea(int startArea, int endArea)
        // {
        //     if (startArea == endArea)
        //         return startArea; // Already at the destination area
        //
        //     var queue = new NativeQueue<int>(Allocator.Temp);
        //     var visited = new NativeHashSet<int>(4, Allocator.Temp);
        //     var parentMap = new NativeHashMap<int, int>(4, Allocator.Temp);
        //
        //     queue.Enqueue(startArea);
        //     visited.Add(startArea);
        //
        //     while (queue.Count > 0)
        //     {
        //         int currentArea = queue.Dequeue();
        //         var currentNav = Areas[currentArea].Connections;
        //
        //         foreach (var neighborArea in currentNav.GetKeyArray(Allocator.Temp))
        //         {
        //             if (visited.Contains(neighborArea)) continue;
        //
        //             visited.Add(neighborArea);
        //             parentMap[neighborArea] = currentArea;
        //             queue.Enqueue(neighborArea);
        //
        //             // If we reached the target area, trace back the next step
        //             if (neighborArea == endArea)
        //             {
        //                 int nextArea = endArea;
        //                 while (parentMap[nextArea] != startArea)
        //                 {
        //                     nextArea = parentMap[nextArea];
        //                 }
        //                 return nextArea;
        //             }
        //         }
        //     }
        //
        //     return -1; // Should never happen if path exists
        // }

        public void Dispose()
        {
            if(Cells.IsCreated)
                Cells.Dispose();
            if (Areas.IsCreated)
            {
                foreach(var area in Areas)
                    area.Value.Dispose();
                Areas.Dispose();
            }
            if(ChangeAreas.IsCreated)
                ChangeAreas.Dispose();
        }
        
        public void Init()
        {
            Dispose();
            ChangeAreas = new (4, Allocator.Persistent);
            Areas = new (8, Allocator.Persistent);
            Cells = new (64, Allocator.Persistent);
        }

        public bool IsWalkable(int2 targetCell) => (GetCellType(targetCell) & CellType.Walkable) != CellType.Blocked;
    }

    internal struct WeightSorter : IComparer<int>
    {
        NativeHashMap<int, int> fScores;
        public WeightSorter(NativeHashMap<int, int> fScores)
        {
            this.fScores = fScores;   
        }

        public int Compare(int x, int y)
        {
            return fScores[x].CompareTo(fScores[y]);
        }
    }
    
    internal struct WeightSorter2 : IComparer<int2>
    {
        NativeHashMap<int2, int> fScores;
        public WeightSorter2(NativeHashMap<int2, int> fScores)
        {
            this.fScores = fScores;   
        }

        public int Compare(int2 x, int2 y)
        {
            return fScores[x].CompareTo(fScores[y]);
        }
    }
}