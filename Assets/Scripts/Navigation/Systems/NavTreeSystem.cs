using Unity.Collections;
using Unity.Entities;

namespace Navigation
{
    partial struct NavTreeSystem : ISystem
    {
        public void OnDestroy(ref SystemState state)
        {
            foreach(var tree in SystemAPI.Query<NavTree>())
                tree.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, e) in SystemAPI.Query<GridStructure>()
                                                      .WithNone<NavTree>()
                                                      .WithEntityAccess())
            {
                ecb.AddComponent<NavTree>(e);
            }
            
            foreach (var (tree, e) in SystemAPI.Query<NavTree>()
                                                      .WithNone<GridStructure>()
                                                      .WithEntityAccess())
            {
                tree.Dispose();
                ecb.RemoveComponent<NavTree>(e);
            }
            ecb.Playback(state.EntityManager);
            
            // foreach (var (grid, navTree, rooms) in SystemAPI.Query<GridStructure, RefRW<NavTree>, RoomModel>()
            //                                          .WithAll<PendingGridUpdate>())
            // {
            //     navTree.ValueRW.BuildTree(grid, rooms);
            // }
            new NavTreeRecalculationJob
            {
                treeLookup = SystemAPI.GetComponentLookup<NavTree>(false),
                roomLookup = SystemAPI.GetComponentLookup<RoomModel>(true)
            }.Run();
        }
    }
}