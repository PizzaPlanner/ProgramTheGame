using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space
{
    public struct Crew: IBufferElementData
    {
        public Entity Member;
    }

    public struct CrewMember : IComponentData { }

    partial struct CrewSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach(var (_, e) in SystemAPI.Query<GridStructure>().WithNone<Crew>().WithEntityAccess())
                ecb.AddBuffer<Crew>(e);
            
            var allCrews = SystemAPI.QueryBuilder().WithAll<Crew>().Build().ToEntityArray(Allocator.Temp);
            foreach (var (gridMember, ltw, e) in SystemAPI.Query<OnGrid, LocalToWorld>()
                                                             .WithChangeFilter<OnGrid>().WithAll<CrewMember>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<Parent>(e) && SystemAPI.GetComponent<Parent>(e).Value == gridMember.Grid)
                    continue;
                
                ecb.AddComponent(e, new Parent{Value = gridMember.Grid});
                if (SystemAPI.GetComponentLookup<LocalToWorld>().TryGetComponent(gridMember.Grid, out var gridLtw))
                    ecb.SetComponent(e, LocalTransform.FromMatrix(math.mul(math.inverse(gridLtw.Value), ltw.Value)));
                else
                    ecb.SetComponent(e, LocalTransform.FromMatrix(ltw.Value));
                foreach (var grid in allCrews)
                {
                    var oldCrew = SystemAPI.GetBuffer<Crew>(grid);
                    for (int i = 0; i < oldCrew.Length; i++)
                        if(oldCrew[i].Member == e)
                            oldCrew.RemoveAt(i--);
                }
                
                if(gridMember.Grid == Entity.Null)
                    continue;
                var crew = SystemAPI.GetBuffer<Crew>(gridMember.Grid);
                crew.Add(new Crew{Member = e});
            }
        }
    }
}