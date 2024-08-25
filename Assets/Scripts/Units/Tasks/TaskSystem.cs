using Core.Grid;
using Identifiables;
using Unity.Entities;

namespace Units
{
    partial struct ClearDanglingDependenciesJob : IJobEntity
    {
        public EntityStorageInfoLookup storageLookup;
        void Execute(ref DynamicBuffer<TaskProvider> providedTasks)
        {
            for (var i = 0; i < providedTasks.Length; i++)
            {
                var task = providedTasks[i];
                if (!storageLookup.Exists(task.Task))
                    providedTasks.RemoveAt(i--);
            }
        }
    }
        
    partial struct TaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var taskLookup = SystemAPI.GetComponentLookup<Task>(true);
            var requirementLookup = SystemAPI.GetComponentLookup<TaskRequirement>(true);
            var crewLookup = SystemAPI.GetBufferLookup<Crew>(true);
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var idleTask = Identifieable.FromName("IdleTask").SpawnablePrefab;
            state.Dependency = new ClearDanglingDependenciesJob
            {
                storageLookup = SystemAPI.GetEntityStorageInfoLookup()
            }.ScheduleParallel(state.Dependency);
            state.Dependency = new TodoPriorityJob
            {
                requirementLookup = requirementLookup,
                taskLookup = taskLookup,
                workedOnLookup = SystemAPI.GetComponentLookup<WorkedOn>(),
                IdleTaskPrefab = idleTask
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