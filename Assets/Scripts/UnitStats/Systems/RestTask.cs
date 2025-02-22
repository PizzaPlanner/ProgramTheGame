using Grids;
using Identifiables;
using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Task = Units.Task;

namespace Core.Equipment
{
    public struct RestTask : IComponentData 
    {
        public State RestState;
        
        public enum State
        {
            LookingForBed,
            Approaching,
            Resting
        }
        
    }
    
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    [WithAll(typeof(RestTask))]
    partial struct FindRestTargetJob : IJobEntity
    {
        public uint currentTick;
        public ComponentLookup<Stamina> staminaLookup;
        public BufferLookup<Statuses> statsLookup;
        public ComponentLookup<Bed> bedLookup;
        [ReadOnly] public ComponentLookup<CGrid> navLookup;
        [ReadOnly] public ComponentLookup<OnGrid> onGridLookup;
        void Execute(ref Task task, ref RestTask rest, ref DynamicBuffer<TaskProvider> dependencies, EnabledRefRO<WorkedOn> workedOn)
        {
            if (!staminaLookup.HasComponent(task.Executor))
            {
                task.Origin = default;
                return;
            }
            ref var stamina = ref staminaLookup.GetRefRW(task.Executor).ValueRW;
            if(stamina.IsExhausted)
                task.Priority = TaskPriority.Emergency;
            else if(stamina.IsTired)
                task.Priority = TaskPriority.HighPriority;
            else
                task.Priority = TaskPriority.Idle;
            switch (rest.RestState)
            {
                case RestTask.State.LookingForBed:
                    if (bedLookup.TryGetComponent(stamina.CurrentBed, out var bed)
                        && bed.Owner == task.Executor
                        && onGridLookup.TryGetComponent(stamina.CurrentBed, out var bedGrid)
                        && bedGrid.Grid == task.CrewProvider)
                    {
                        task.Target = stamina.CurrentBed;
                        rest.RestState = RestTask.State.Approaching;
                        return;
                    }
                    foreach(var entity in navLookup[task.CrewProvider].GetAllEntities())
                        if (bedLookup.TryGetComponent(entity, out var bed2) &&
                            (bed2.Owner == default || bed2.Owner == task.Origin))
                        {
                            stamina.CurrentBed = entity;
                            bed2.Owner = task.Origin;
                            bedLookup[entity] = bed2;
                            return;
                        }
                    stamina.CurrentBed = default;
                    task.Target = task.Origin;
                    rest.RestState = RestTask.State.Approaching;
                    break;
                case RestTask.State.Approaching:
                    if (workedOn.ValueRO)
                        rest.RestState = RestTask.State.Resting;
                    break;
                case RestTask.State.Resting:
                    statsLookup[task.Executor].Add(Identifiable.FromName("Resting"), currentTick);
                    task.Priority = TaskPriority.HighPriority;
                    if (stamina.IsFull || !workedOn.ValueRO)
                        task.Origin = default;
                    break;
            }
        }
    }
}