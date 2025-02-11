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
            foreach (var (grid, mass, rooms, nav) in SystemAPI.Query<Grids.CGrid, MassModel, RoomModel, NavTree>())
            {
                grid.Dispose();
                mass.Dispose();
                rooms.Dispose();
                nav.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (loc, e) in SystemAPI.Query<LocationPopulationSettings>()
                                              .WithAll<Sector>()
                                              .WithEntityAccess())
            {
                Blueprint.Spawn(ecb, loc.BlueprintToSpawn, e);
                ecb.RemoveComponent<LocationPopulationSettings>(e);
            }
            
            foreach (var (_, e) in SystemAPI.Query<Sector>().WithNone<CGrid>().WithEntityAccess())
            {
                ecb.AddComponent(e, new CGrid(Allocator.Persistent));
            
                var model = new MassModel(new NativeElementMap(Allocator.Persistent, 0, 1));
                ecb.AddComponent(e, model);
                
                var tree = new NavTree();
                tree.Init();
                ecb.AddComponent(e, tree);
                
                var roomModel = new RoomModel();
                roomModel.Initialize();
                ecb.AddComponent(e, roomModel);
                
                Debug.Log($"Populated {e}");
            }

            foreach (var (grid, mass, rooms, nav, e) in SystemAPI.Query<CGrid, MassModel, RoomModel, NavTree>().WithNone<Sector>().WithEntityAccess())
            {
                Debug.Log($"Depopulated {e}");
                
                grid.Dispose();
                ecb.RemoveComponent<CGrid>(e);
                
                mass.Dispose();
                ecb.RemoveComponent<MassModel>(e);
                
                rooms.Dispose();
                ecb.RemoveComponent<RoomModel>(e);
                
                nav.Dispose();
                ecb.RemoveComponent<NavTree>(e);
            }
        }
    }
}