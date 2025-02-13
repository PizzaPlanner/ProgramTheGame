using Grids;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.AddressableAssets;

[DisableAutoCreation]
[UpdateInGroup(typeof(GridUpdateSystemGroup))]
partial struct GridMeshSystem : ISystem
{
    struct CMM : IComponentData { }
    public void OnUpdate(ref SystemState state)
    {
        var defaultMaterial = Addressables.LoadAssetAsync<Material>("TestMat").WaitForCompletion();
        var graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        var newQuery = SystemAPI.QueryBuilder().WithAll<CGrid>().WithNone<MaterialMeshInfo>().Build();
        foreach (var e in newQuery.ToEntityArray(Allocator.Temp))
        {
            var grid = SystemAPI.GetComponent<CGrid>(e);
            var mesh = CellMesh.GenerateChunkMesh(grid);
            var meshId = graphicsSystem.RegisterMesh(mesh);
            state.EntityManager.AddComponent<CMM>(e);
            RenderMeshUtility.AddComponents(e, state.EntityManager, new RenderMeshDescription
            {
                FilterSettings  = RenderFilterSettings.Default
            }, new MaterialMeshInfo
            {
                MeshID  = meshId,
                MaterialID = graphicsSystem.RegisterMaterial(defaultMaterial)
            });
        }
        
        
        foreach (var (grid, mme, e) in SystemAPI.Query<CGrid, MaterialMeshInfo>().WithEntityAccess())
        {
            var mesh = graphicsSystem.GetMesh(mme.MeshID);
            CellMesh.GenerateChunkMesh(grid, mesh);
        }
    }
}