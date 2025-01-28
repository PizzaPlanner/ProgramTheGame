using Identifiables;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

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
        [ReadOnly] public ComponentLookup<OnGrid> onGridLookup;
        void Execute(Entity e, ref Task task, ref DynamicBuffer<TaskProvider> dependencies, EnabledRefRO<WorkedOn> workedOn)
        {
            if(bedLookup.HasComponent(task.Target) || !staminaLookup.HasComponent(task.Executor))
                return;
            
            ref var stamina = ref staminaLookup.GetRefRW(task.Executor).ValueRW;
            if(workedOn.ValueRO)
                return;
            
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
        }
    }
}