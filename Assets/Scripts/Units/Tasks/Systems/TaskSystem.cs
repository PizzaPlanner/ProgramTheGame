using Core;
using Core.Grid;
using Identifiables;
using Navigation;
using Space;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Units
{
    partial struct TaskProviderConsistencyJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<TaskUniquenessGroup> uniquenessLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Task> taskLookup;
        void Execute(ref DynamicBuffer<TaskProvider> providedTasks)
        {
            var presentUids = new NativeHashSet<Identifiable>(4, Allocator.Temp);
            for (var i = 0; i < providedTasks.Length; i++)
            {
                if (!taskLookup.TryGetComponent(providedTasks[i].Task, out var task))
                {
                    providedTasks.RemoveAt(i--);
                    continue;
                }

                if (uniquenessLookup.TryGetComponent(providedTasks[i].Task, out var uid) && !presentUids.Add(uid.Group))
                {
                    task.Origin = Entity.Null;
                    taskLookup[providedTasks[i].Task] = task;
                    providedTasks.RemoveAt(i--);
                    continue;
                }
            }
        }
    }
        
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct TaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var taskLookup = SystemAPI.GetComponentLookup<Task>(false);
            var requirementLookup = SystemAPI.GetComponentLookup<TaskRequirement>(true);
            var crewLookup = SystemAPI.GetBufferLookup<Crew>(true);
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            state.Dependency = new TaskProviderConsistencyJob
            {
                taskLookup = taskLookup,
                uniquenessLookup = SystemAPI.GetComponentLookup<TaskUniquenessGroup>(true),
                
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new TodoPriorityJob
            {
                requirementLookup = requirementLookup,
                taskLookup = taskLookup,
                workedOnLookup = SystemAPI.GetComponentLookup<WorkedOn>(),
                navTreeLookup  = SystemAPI.GetComponentLookup<NavTree>(true),
                currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick
            }.Schedule(state.Dependency);
            state.Dependency = new TaskRequestJob
            {
                todoLookup = SystemAPI.GetBufferLookup<ToDos>(),
                storageLookup = SystemAPI.GetEntityStorageInfoLookup(),
                crewLookup = crewLookup,
                ecb = ecb
            }.ScheduleParallel(state.Dependency);
        }
    }
}