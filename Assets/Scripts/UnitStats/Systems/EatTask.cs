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
    public struct EatTask : IComponentData
    {
        public int2 BestTile;
    }
    
    partial struct FindFoodTargetJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public ComponentLookup<Stomach> stomachLookup;
        [ReadOnly] public ComponentLookup<OnGrid> onGridLookup;
        [ReadOnly] public ComponentLookup<Food> foodLookup;
        [ReadOnly] public NativeArray<Entity> foodEntities;
        void Execute(ref Task task, ref EatTask eater)
        {
            if(!stomachLookup.TryGetComponent(task.Executor, out var stomach))
                return;

            if (!stomach.IsHungry)
            {
                task.Origin = default;
                return;
            }
            
            task.Priority = stomach.IsStarving 
                ? TaskPriority.Emergency 
                : TaskPriority.MediumPriority;
            
            if(task.Target != default 
               && foodLookup.TryGetComponent(task.Target, out var food) 
               && food.Nutrition > 0)
                return;
            
            foreach (var foodEntity in foodEntities)
            {
                if(!onGridLookup.TryGetComponent(foodEntity, out var onGrid) || onGrid.Grid != task.CrewProvider)
                    continue;
                if(foodEntity == task.Target) 
                    continue;
                task.Target = foodEntity;
                return;
            }
            task.Target = default;
        }
    }
}