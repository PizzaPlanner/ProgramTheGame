using Grids;
using Navigation;
using Units;
using Unity.Collections;
using Unity.Entities;

namespace Core.Equipment
{
    public struct RestTask : IComponentData { }
    
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    [WithAll(typeof(RestTask))]
    partial struct FindRestTargetJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public ComponentLookup<Stamina> staminaLookup;
        [ReadOnly] public ComponentLookup<Bed> bedLookup;
        [ReadOnly] public ComponentLookup<CGrid> navLookup;
        [ReadOnly] public ComponentLookup<OnGrid> onGridLookup;
        void Execute(Entity e, ref Task task, ref DynamicBuffer<TaskProvider> dependencies, EnabledRefRO<WorkedOn> workedOn)
        {
            if(bedLookup.HasComponent(task.Target) || !staminaLookup.HasComponent(task.Executor))
                return;
            
            ref var stamina = ref staminaLookup.GetRefRW(task.Executor).ValueRW;
            if(workedOn.ValueRO)
                return;

            if (bedLookup.TryGetComponent(stamina.CurrentBed, out var bed)
                && bed.Owner == task.Executor
                && onGridLookup.TryGetComponent(stamina.CurrentBed, out var bedGrid)
                && bedGrid.Grid == task.CrewProvider)
            {
                task.Target = stamina.CurrentBed;
                return;
            }
            
            stamina.CurrentBed = default;
            if (!stamina.IsTired)
            {
                task.Origin = default;
                return;
            }
            task.Priority = stamina.IsExhausted 
                ? TaskPriority.HighPriority 
                : TaskPriority.MediumPriority;
            
            // Find Bed???
            foreach(var entity in navLookup[task.CrewProvider].GetAllEntities())
                if(bedLookup.TryGetComponent(entity, out var bed2) && (bed2.Owner == default || bed2.Owner == task.Origin))
                    stamina.CurrentBed = entity;
            
            // Sleep on floor
            if(stamina.IsExhausted && stamina.CurrentBed == default)
                task.Target = task.Origin;
        }
    }
}