using Equipment;
using Identifiables;
using Unity.Entities;
using Unity.Transforms;

namespace Units
{
    public struct DispenseUnitTask : IComponentData
    {
        public Identifiable UnitPrefab;
    }

    [RequireMatchingQueriesForUpdate]
    partial struct DispenseUnitTaskSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (task, dutask, progress, e) in SystemAPI.Query<RefRW<Task>, DispenseUnitTask, RefRW<TaskProgress>>().WithAll<WorkedOn>().WithEntityAccess())
            {
                progress.ValueRW.PartialTicks += 1;
                if(!progress.ValueRW.IsDone)
                    continue;
                SystemAPI.GetComponentRW<UnitDispenser>(task.ValueRW.Origin).ValueRW.IsDispensed  = true;
                var newUnit = ecb.Instantiate(dutask.UnitPrefab.SpawnablePrefab);
                ecb.SetComponent(newUnit, SystemAPI.GetComponent<LocalTransform>(task.ValueRW.Origin));
                ecb.SetComponent(newUnit, SystemAPI.GetComponent<OnGrid>(task.ValueRW.Origin));
                ecb.DestroyEntity(e);
            }
        }
    }
}