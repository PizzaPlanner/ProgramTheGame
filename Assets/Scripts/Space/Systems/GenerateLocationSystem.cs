using Unity.Collections;
using Unity.Entities;
using Grids;
using Navigation;
using UnityEngine;

namespace Space.Systems
{
    partial struct GenerateLocationsSystem : ISystem
    {
        public void OnDestroy(ref SystemState state)
        {
            foreach (var (grid, mass, nav) in SystemAPI.Query<Grids.CGrid, MassModel, NavTree>())
            {
                grid.Dispose();
                mass.Dispose();
                nav.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var (_, e) in SystemAPI.Query<Sector>().WithNone<CGrid>().WithEntityAccess())
            {
                ecb.AddComponent(e, new CGrid(Allocator.Persistent));
            
                var model = new MassModel(new NativeElementMap(Allocator.Persistent, 0, 1));
                ecb.AddComponent(e, model);
                
                var tree = new NavTree();
                tree.Init();
                ecb.AddComponent(e, tree);
                
                Debug.Log($"Populated {e}");
            }

            foreach (var (grid, mass, nav, e) in SystemAPI.Query<CGrid, MassModel, NavTree>().WithNone<Sector>().WithEntityAccess())
            {
                Debug.Log($"Depopulated {e}");
                
                grid.Dispose();
                ecb.RemoveComponent<CGrid>(e);
                
                mass.Dispose();
                ecb.RemoveComponent<MassModel>(e);
                
                nav.Dispose();
                ecb.RemoveComponent<NavTree>(e);
            }
        }
    }
}