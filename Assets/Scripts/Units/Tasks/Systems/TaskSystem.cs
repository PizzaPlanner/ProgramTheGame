using Core;
using Navigation;
using Space;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Units
{
    [BurstCompile]
    partial struct TaskProviderConsistencyJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<TaskUniquenessGroup> uniquenessLookup;
        [NativeDisableParallelForRestriction]
        public ComponentLookup<Task> taskLookup;
        
        [BurstCompile]
        void Execute(ref DynamicBuffer<TaskProvider> providedTasks)
        {
            if(providedTasks.Length == 0)
                return;
            
            for (var i = 0; i < providedTasks.Length; i++)
            {
                if (!taskLookup.TryGetComponent(providedTasks[i].Task, out var task))
                {
                    providedTasks.RemoveAt(i--);
                    continue;
                }

                if (uniquenessLookup.TryGetComponent(providedTasks[i].Task, out var uid))
                {
                    for (int j = i + 1; j < providedTasks.Length; j++)
                        if (uniquenessLookup.TryGetComponent(providedTasks[j].Task, out var uid2) &&
                            uid.Group == uid2.Group)
                        {
                            taskLookup.GetRefRW(providedTasks[j].Task).ValueRW.Origin = default;
                            providedTasks.RemoveAt(j--);
                        }
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