using Core;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace Navigation
{
    partial struct NavTreeSystem : ISystem
    {
        public void OnDestroy(ref SystemState state)
        {
            foreach (var (tree, layer) in SystemAPI.Query<NavTree, NavLayers>())
            {
                tree.Dispose();
                layer.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, e) in SystemAPI.Query<GridStructure>()
                                                      .WithNone<NavTree>()
                                                      .WithEntityAccess())
            {
                ecb.AddComponent<NavTree>(e);
                ecb.AddComponent(e, new NavLayers(Allocator.Persistent));
            }
            
            foreach (var (tree, layers, e) in SystemAPI.Query<NavTree, NavLayers>()
                                                      .WithNone<GridStructure>()
                                                      .WithEntityAccess())
            {
                layers.Dispose();
                tree.Dispose();
                ecb.RemoveComponent<NavTree>(e);
                ecb.RemoveComponent<NavLayers>(e);
            }
            ecb.Playback(state.EntityManager);
            
            new NavTreeRecalculationJob
            {
                treeLookup = SystemAPI.GetComponentLookup<NavTree>(false),
                currentTick = SystemAPI.GetSingleton<SimulationState>().LastSimulationTick
            }.Run();
        }
    }
}