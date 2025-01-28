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
    public struct NavCell
    {
        public const int NO_PATH = 1000000;
        public int4 CostTRBL;
        public bool4 HasPath => CostTRBL != 0;
        public int SelfCost;
        public FixedList64Bytes<int> Areas;
        
        public int CostLeft => CostTRBL.w;
        public int CostRight => CostTRBL.y;
        public int CostUp => CostTRBL.x;
        public int CostDown => CostTRBL.z;
    }
    public struct NavTree : ICleanupComponentData, IDisposable
    {
        // TODO: ChangeAreas data not used
        internal UnsafeList<GridBounds> ChangeAreas;
        internal UnsafeHashMap<int2, NavCell> Cells;
        internal UnsafeParallelMultiHashMap<int, int2> CellsByArea;
        internal UnsafeParallelHashMap<int, GridBounds> BoundsByArea;
        internal UnsafeParallelHashMap<int, UnsafeHashSet<int>> AreaConnections;
        public GridBounds Bounds;
        public int MaxLevel => (int)math.log2(math.ceilpow2(math.max(Bounds.Scale.x, Bounds.Scale.y)));
        public bool IsDirty => ChangeAreas.Length > 0 || Cells.IsEmpty;
        public uint LastUpdate;

        public const int LEVEL_LIMIT = 10;
        public int LevelOf(int area) => area % LEVEL_LIMIT;

        public int3 ParentOf(int3 cell)
        {
            var relativeCell = cell.xy - Bounds.Min;
            var result = new int3(cell.xy - relativeCell.xy % (1 << (cell.z + 1)), cell.z + 1);
            return result;
        }

        public GridBounds GetBoundsAtLevel(int2 absoluteCell, int level)
        {
            var min = ParentOf(new int3(absoluteCell, level - 1)).xy;
            var max = min + (1 << level) - 1;
            return new GridBounds
            {
                Min = min,
                Max = max
            };
        }

        // For finding paths to walls, which are not on the navtree
        public bool NavigateAdjacent(int2 start, int2 blockedEnd, ref NativeList<int2> path)
        {
            Span<int2> neighbours = stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)};
            foreach (var nb in neighbours)
            {
                var cell = blockedEnd + nb;
                if(Cells.TryGetValue(cell, out _))
                    return Navigate(start, cell, ref path);
            }
            return false;
        }

        public void SetAreaDirty(GridBounds bounds)
        {
            ChangeAreas.Add(bounds);
        }

        public bool IsReachable(int2 start, int2 end, bool allowAdjacent, out int level)
        {
            if (allowAdjacent)
            {
                foreach(var nb in stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)})
                    if(IsReachable(start, end + nb, false, out level))
                        return true;
            }
            level = 0;
            if(!Cells.TryGetValue(end, out var toCell))
                return false;
            if(!Cells.TryGetValue(start, out var fromCell))
                return false;
            for (; level < MaxLevel; level++)
            {
                if(toCell.Areas[level] == fromCell.Areas[level])
                    return true;
            }
            return false;
        }
        
        public bool Navigate(int2 start, int2 end, ref NativeList<int2> path)
        {
            if(!IsReachable(start, end, false, out var level))
                return false;
            if (level < 2)
            {
                AStarDirect(start, end, ref path);
                return true;
            }
            var fromCell = Cells[start];
            var toCell = Cells[end];
            var topLevel = AStar(fromCell, toCell, level - 1);
            var nextArea = topLevel[0];
            AStarToArea(start, nextArea, ref path);
            path.Add(end);
            return true;
        }

        unsafe void AStarDirect(int2 start, int2 end, ref NativeList<int2> path)
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
                    if(closed.Contains(next) || !Cells.ContainsKey(next))
                        continue;
                    var gScore = gScores[current] + 1;
                    var previousGScore = gScores.TryGetValue(next, out var s) ? s : int.MaxValue; 
                    if (gScore < previousGScore)
                    {
                        var h = (int)math.distance(GridStructure.CellCenterToLocal(end), GridStructure.CellCenterToLocal(next));
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
        
        unsafe void AStarToArea(int2 start, int nextArea, ref NativeList<int2> path)
        {
            // AStar to the best cell from targetArea directly reachable by startArea
            var targetLevelIndex = LevelOf(nextArea) - 1;
            var currentArea = Cells[start].Areas[targetLevelIndex];

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
                var currentCell = Cells[current];
                if(currentCell.Areas[targetLevelIndex] == nextArea)
                    break;
                if(currentCell.Areas[targetLevelIndex] != currentArea)
                    continue;
                if(!closed.Add(current))
                    continue;
                foreach (var neighbour in neighbours)
                {
                    var next = current + neighbour;
                    if(closed.Contains(next) || !Cells.ContainsKey(next))
                        continue;
                    var gScore = gScores[current] + 1;
                    var previousGScore = gScores.TryGetValue(next, out var s) ? s : int.MaxValue; 
                    if (gScore < previousGScore)
                    {
                        var h = (int)math.distance(BoundsByArea[nextArea].Center, GridStructure.CellCenterToLocal(next));
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

        NativeList<int> AStar(NavCell start, NavCell end, int areaLevel)
        {
            var startArea = start.Areas[areaLevel];
            var endArea = end.Areas[areaLevel];
            var open = new NativeList<int>(32, Allocator.Temp);
            var closed = new NativeHashSet<int>(32, Allocator.Temp);
            var bestSourceNodeMap = new NativeHashMap<int, int>(32, Allocator.Temp);
            var gScores = new NativeHashMap<int, int>(32, Allocator.Temp);
            gScores[startArea] = 0;
            var fScores = new NativeHashMap<int, int>(32, Allocator.Temp);
            fScores[startArea] = 0;
            open.Add(startArea);
            while (!open.IsEmpty)
            {
                var current = open[0];
                open.RemoveAt(0);
                if(current == endArea)
                    break;
                if(!closed.Add(current))
                    continue;
                foreach (var connection in AreaConnections[current])
                {
                    if(closed.Contains(connection) || LevelOf(connection) != LevelOf(current))
                        continue;
                    var gScore = gScores[current] + (2 << LevelOf(connection));
                    var previousGScore = gScores.TryGetValue(connection, out var s) ? s : int.MaxValue; 
                    if (gScore < previousGScore)
                    {
                        var h = (int)math.distance(BoundsByArea[connection].Center, BoundsByArea[endArea].Center);
                        bestSourceNodeMap[connection] = current;
                        gScores[connection] = gScore;
                        fScores[connection] = gScore + h;
                        open.Add(connection);
                    }
                }
                open.Sort(new WeightSorter(fScores));
            }

            if (bestSourceNodeMap.ContainsKey(endArea))
            {
                var areas = new NativeList<int>(Allocator.Temp);
                var fromArea = endArea;
                while (fromArea != startArea)
                {
                    CellsByArea.TryGetFirstValue(fromArea, out var cell, out _);
                    areas.Add(fromArea);
                    fromArea = bestSourceNodeMap[fromArea];
                }
                areas.AsArray().AsSpan().Reverse();
                // var p = $"{startArea} -> ";
                // foreach(var a in areas)
                //     p += $"{a} -> ";
                // Debug.Log(p[..^4]);
                return areas;
            }
            return default;
        }

        public void Dispose()
        {
            if(Cells.IsCreated)
                Cells.Dispose();
            if(CellsByArea.IsCreated)
                CellsByArea.Dispose();
            if(BoundsByArea.IsCreated)
                BoundsByArea.Dispose();
            if(ChangeAreas.IsCreated)
                ChangeAreas.Dispose();
            if (AreaConnections.IsCreated)
            {
                foreach(var entry in AreaConnections.GetValueArray(Allocator.Temp))
                    entry.Dispose();
                AreaConnections.Dispose();
            }
        }
        
        public void Init(GridBounds bounds)
        {
            Dispose();
            Bounds = bounds;
            ChangeAreas = new (4, Allocator.Persistent);
            CellsByArea = new (bounds.Size / 4, Allocator.Persistent);
            Cells = new (bounds.Size / 4, Allocator.Persistent);
            AreaConnections = new (bounds.Size / 16, Allocator.Persistent);
            BoundsByArea = new(bounds.Size / 16, Allocator.Persistent);
        }
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