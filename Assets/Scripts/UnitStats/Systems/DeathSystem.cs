using Core.Status;
using Identifiables;
using Units;
using Unity.Collections;
using Unity.Entities;

namespace Core.Equipment
{
    [UpdateInGroup(typeof(TickBasedSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    partial struct DeathSystem : ISystem
    {
        static readonly Identifiable DeadStatus = Identifiable.FromName("Dead");
        struct Handled : IComponentData { }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (stats, e) in SystemAPI.Query<DynamicBuffer<Statuses>>()
                                                .WithAll<Dead>()
                                                .WithNone<Handled>().WithEntityAccess())
            {
                stats.Clear();
                stats.Add(DeadStatus);
                ecb.AddComponent<Handled>(e);
                if (SystemAPI.HasBuffer<ToDos>(e))
                    SystemAPI.GetBuffer<ToDos>(e).Clear();
                if (SystemAPI.HasBuffer<TaskProvider>(e))
                {
                    var buffer = SystemAPI.GetBuffer<TaskProvider>(e);
                    foreach(var task in buffer)
                        ecb.DestroyEntity(task.Task);
                }
                    
            }
            ecb.Playback(state.EntityManager);
        }
    }
}