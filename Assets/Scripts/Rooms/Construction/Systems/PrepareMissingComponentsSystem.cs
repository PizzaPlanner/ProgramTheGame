using Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct PrepareGridComponentsSystem : ISystem
{
    public void OnDestroy(ref SystemState state)
    {
        foreach (var model in SystemAPI.Query<RoomModel>())
            model.Dispose();
        
        foreach (var model in SystemAPI.Query<MassModel>())
            model.Dispose();
        
        foreach (var (tree, layer) in SystemAPI.Query<NavTree, NavLayers>())
        {
            tree.Dispose();
            layer.Dispose();
        }
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var (grid, e) in SystemAPI.Query<GridStructure>().WithNone<MassModel>().WithEntityAccess())
        {
            var model = new MassModel(new NativeElementMap(Allocator.Persistent, grid.BoundsMin, grid.BoundsMax));
            ecb.AddComponent(e, model);
        }

        foreach (var (model, e) in SystemAPI.Query<MassModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecb.RemoveComponent<MassModel>(e);
        }
        
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
        
        // Remove dangling rooms
        foreach (var (model, e) in SystemAPI.Query<RoomModel>().WithNone<LocalToWorld>().WithEntityAccess())
        {
            model.Dispose();
            ecb.RemoveComponent<RoomModel>(e);
        }

        // Initialize new rooms
        foreach (var (_, e) in SystemAPI.Query<GridStructure>().WithNone<RoomModel>().WithEntityAccess())
        {
            var roomModel = new RoomModel();
            roomModel.Initialize();
            ecb.AddComponent(e, roomModel);
        }
    }
}