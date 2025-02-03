using Grids;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Equipment
{
    public struct BreatheTask : IComponentData
    {
        public int2 BestTile;
    }
    
    [WithAll(typeof(LocalTransform))]
    partial struct FindBreathableTargetJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public ComponentLookup<MassModel> massLookup;
        [ReadOnly] public ComponentLookup<Lungs> lungsLookup;
        void Execute(Entity e, ref Task task, ref BreatheTask breather)
        {
            if(!lungsLookup.TryGetComponent(task.Executor, out var lungs))
                return;
            if(!massLookup.TryGetComponent(task.CrewProvider, out var massModel))
                return;

            if (!lungs.IsLowOxygen)
            {
                task.Origin = default;
                return;
            }
            
            task.Priority = lungs.IsSuffocating 
                ? TaskPriority.ImmediateThreat 
                : TaskPriority.MediumPriority;
            
            if(task.Target != default && GetPriority(massModel, 0, breather.BestTile, lungs) > 0)
                return;
            
            if(!transformLookup.TryGetComponent(task.Executor, out var execPos))
                return;
            
            task.Target = default;
            var maxSearchDistanceSq = 30 * 30; // TODO: Calculate based on time before suffocating 
            var priorityQueue = new NativeQueue<int2>(Allocator.Temp);
            var visitedCells = new NativeHashSet<int2>(16, Allocator.Temp);
            var bestPriority = 0f;
            var bestCell = (int2)int.MinValue;
            var startCell = CGrid.LocalToCell(execPos.Position);
            priorityQueue.Enqueue(startCell);
            Span<int2> neighbours = stackalloc int2[] { new int2(1, 0), new int2(-1, 0), new int2(0, 1), new int2(0, -1)};
            while (bestPriority <= 0 && priorityQueue.TryDequeue(out var currentCell))
            {
                if(!visitedCells.Add(currentCell))
                    continue;
                foreach (var nb in neighbours)
                {
                    var nextCell = currentCell + nb;
                    var distanceSq = math.distancesq(startCell, nextCell);
                    if(distanceSq > maxSearchDistanceSq)
                        continue;
                    var nextPrio = GetPriority(massModel, distanceSq, nextCell, lungs);
                    if (nextPrio > bestPriority)
                    {
                        bestPriority = nextPrio;
                        bestCell = nextCell;
                    }
                    else
                        priorityQueue.Enqueue(nextCell);
                }
            }
            if(bestPriority <= 0)
                return;
            
            breather.BestTile = bestCell;
            ref var transform = ref transformLookup.GetRefRW(e).ValueRW;
            transform.Position = CGrid.CellCenterToLocal(bestCell);
            task.Target = e;
        }

        float GetPriority(MassModel model, float distanceSq, int2 cell, Lungs lungs)
        {
            if(!model.IsInBounds(cell))
                return 0;
            var header = model.AbsoluteCell(cell);
            if(header.HeaderType != MassHeader.VolumeType.Contained)
                return 0;
            var minBreathability = 3; // Need at least 3 ticks worth of air to be breathable
            var breathability = header.GetMass(lungs.Inhales) * 1f / lungs.ConsumptionPerTick - minBreathability;
            return breathability / (distanceSq + 1);
        }
    }
}