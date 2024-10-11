using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Core.Grid
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
            var allCrews = SystemAPI.QueryBuilder().WithAll<Crew>().Build().ToEntityArray(Allocator.Temp);
            foreach (var (gridMember, ltw, e) in SystemAPI.Query<OnGrid, LocalToWorld>()
                                                             .WithChangeFilter<OnGrid>().WithAll<CrewMember>().WithEntityAccess())
            {
                ecb.AddComponent(e, new Parent{Value = gridMember.Grid});
                if (gridMember.Grid != Entity.Null)
                {
                    var gridLtw = SystemAPI.GetComponent<LocalToWorld>(gridMember.Grid);
                    ecb.SetComponent(e, LocalTransform.FromMatrix(math.mul(math.inverse(gridLtw.Value), ltw.Value)));
                }
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
                    return;
                var crew = SystemAPI.GetBuffer<Crew>(gridMember.Grid);
                crew.Add(new Crew{Member = e});
            }
        }
    }
}