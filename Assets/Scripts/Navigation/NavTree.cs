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
        public CellType PathingRestrictions;
    }

    public struct AreaData : IDisposable
    {
        internal UnsafeHashSet<int2> Cells;
        // Cells directly adjacent to this area that are located in other areas.
        internal UnsafeParallelMultiHashMap<int, int2> AdjacentCellsByArea;
        internal GridBounds AABB;
        internal UnsafeHashMap<int, AreaNavigationData> Connections;
        internal CellType Type;

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

        public bool HasPathWithin(int2 start, int2 end, int range, CellType walkableFilter, CellType reachableFilter)
        {
            if(HasPath(start, end, walkableFilter))
                return true;
            for(int x = end.x - range; x <= end.x + range; x++)
            for(int y = end.y - range; y <= end.y + range; y++)
                if(HasLineOfSight(new int2(x, y), end, range, reachableFilter)
                   && HasPath(start, new int2(x, y), walkableFilter))
                    return true;
            return false;
        }
        
        public bool NavigateWithin(int2 start, int2 end, int range, CellType walkableFilter, CellType reachableFilter, ref NativeList<int2> path)
        {
            if(Navigate(start, end, walkableFilter, ref path))
               return true;
            for(int x = end.x - range; x <= end.x + range; x++)
            for(int y = end.y - range; y <= end.y + range; y++)
                if (HasLineOfSight(new int2(x, y), end, range, reachableFilter) 
                    && Navigate(start, new int2(x, y), walkableFilter, ref path))
                    return true;
            return false;
        }
        
        // LOS check using Bresenham’s line algorithm or raycasting
        bool HasLineOfSight(int2 from, int2 to, int maxRange, CellType allowedTypes)
        {
            if (math.distancesq(from, to) > maxRange * maxRange)
                return false;
            
            var dx = math.abs(to.x - from.x);
            var dy = math.abs(to.y - from.y);
            var sx = from.x < to.x ? 1 : -1;
            var sy = from.y < to.y ? 1 : -1;
            var err = dx - dy;

            var pos = from;
            while (math.any(pos != to))
            {
                if (!Cells.TryGetValue(pos, out var c) || (allowedTypes & c.Type) == 0)
                    return false; // Blocked LOS

                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; pos.x += sx; }
                if (e2 < dx) { err += dx; pos.y += sy; }
            }
            return true;
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
        
        public bool HasPath(int2 start, int2 end, CellType walkableFilter)
        {
            if(!Cells.TryGetValue(end, out var toCell) || (toCell.Type & walkableFilter) == CellType.Blocked)
                return false;
            if(!Cells.TryGetValue(start, out var fromCell) || (fromCell.Type & walkableFilter) == CellType.Blocked)
                return false;
            return fromCell.Area == toCell.Area 
                   || (Areas[fromCell.Area].Connections.TryGetValue(toCell.Area, out var connection) 
                       && (connection.PathingRestrictions & walkableFilter) != CellType.Blocked);
        }
        
        public bool Navigate(int2 start, int2 end, CellType allowedTypes, ref NativeList<int2> path)
        {
            if(!HasPath(start, end, allowedTypes))
                return false;
            AStarDirect(start, end, allowedTypes, ref path);
            if (math.all(path[^1] != end))
            {
                Debug.LogError($"AStar {start} to {end} produced invalid result to {path[^1]}");
                return false;
            }
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
            path.AsArray().AsSpan().Reverse();
        }

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

        public void UpdateAreaPathing(int2 location, CellType type)
        {
            if(!Cells.TryGetValue(location, out var cell) || !Areas.TryGetValue(cell.Area, out var area))
                return;
            if(area.Type == type)
                return;
            foreach (var c in area.Cells)
            {
                if(!Cells.TryGetValue(c, out var otherCell))
                    continue;
                otherCell.Type = type;
                Cells[c] = otherCell;
            }
            area.Type = type;
            Areas[cell.Area] = area;
            RecalculateAreaConnections(ref this);
        }
        
        internal static void RecalculateAreaConnections(ref NavTree tree)
        {
            Span<int2> neighbours = stackalloc int2[] { new int2(-1, 0), new int2(0, -1), new int2(0, 1), new int2(1, 0) };
            // Remap adjacent areas
            foreach (var areaId in tree.Areas.GetKeyArray(Allocator.Temp))
            {
                var area = tree.Areas[areaId];
                var connections = area.Connections;
                connections.Clear();
                foreach(var cell in area.Cells)
                foreach(var nb in neighbours)
                    if (tree.Cells.TryGetValue(cell + nb, out var neighbour) && neighbour.Area != areaId)
                    {
                        area.AdjacentCellsByArea.Add(neighbour.Area, cell + nb);
                        connections[neighbour.Area] = new AreaNavigationData
                        {
                            PreviousArea = areaId,
                            PathingRestrictions = area.Type & tree.Areas[neighbour.Area].Type
                        };
                    }
                area.Connections = connections;
                tree.Areas[areaId] = area;
            }
            
            
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
                            var newPathing = navData.PathingRestrictions & otherNavData.PathingRestrictions;

                            if (areaId == otherAreaId)
                                continue;
                            if (area.Connections.ContainsKey(otherAreaId) && (int)newPathing <= (int)area.Connections[otherAreaId].PathingRestrictions)
                                continue;
                            var connections = area.Connections;
                            connections[otherAreaId] = new AreaNavigationData
                            {
                                PreviousArea = neighborAreaId,
                                PathingRestrictions = newPathing
                            };
                            area.Connections = connections;
                            changed = true;
                        }
                    }

                    tree.Areas[areaId] = area;
                }
            }
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