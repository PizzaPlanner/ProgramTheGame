using Core.Grid;
using Unity.Entities;

namespace Units
{
    partial struct TaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var taskLookup = SystemAPI.GetComponentLookup<Task>(true);
            var requirementLookup = SystemAPI.GetComponentLookup<TaskRequirement>(true);
            var crewLookup = SystemAPI.GetBufferLookup<Crew>(true);
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            state.Dependency = new TodoPriorityJob
            {
                requirementLookup = requirementLookup,
                taskLookup = taskLookup,
                workedOnLookup = SystemAPI.GetComponentLookup<WorkedOn>(true)
            }.ScheduleParallel(state.Dependency);
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