using Unity.Collections;
using Unity.Entities;

namespace Units
{
    partial struct TaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var workerQuery = SystemAPI.QueryBuilder().WithAll<ToDos>().Build();
            var workers = workerQuery.ToEntityArray(Allocator.TempJob);
            var taskLookup = SystemAPI.GetComponentLookup<Task>(true);
            state.Dependency = new TodoPriorityJob
            {
                taskLookup = taskLookup,
                workedOnLookup = SystemAPI.GetComponentLookup<WorkedOn>(true)
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new TaskRequestJob
            {
                todoLookup = SystemAPI.GetBufferLookup<ToDos>(),
                upForWork = workers,
                storageLookup = SystemAPI.GetEntityStorageInfoLookup(),
                ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            workers.Dispose(state.Dependency);
        }
    }
}