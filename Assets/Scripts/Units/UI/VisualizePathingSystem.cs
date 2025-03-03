using Grids;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Units
{
    partial struct VisualizePathingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var newQuery = SystemAPI.QueryBuilder().WithAll<UnitPathing>().WithAbsent<MaterialMeshInfo>().Build();
            var mat = Addressables.LoadAssetAsync<Material>("TestMat").WaitForCompletion();
            foreach (var e in newQuery.ToEntityArray(Allocator.Temp))
            {
                var mesh = CellMesh.GenerateChunkMesh(default(NativeArray<int2>));
                var meshId = graphicsSystem.RegisterMesh(mesh);
                var matId = graphicsSystem.RegisterMaterial(mat);
                RenderMeshUtility.AddComponents(e, state.EntityManager, new RenderMeshDescription
                {
                    FilterSettings  = RenderFilterSettings.Default
                }, new MaterialMeshInfo
                {
                    MeshID  = meshId,
                    MaterialID = matId
                });
                state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(e, false);
            }
            
            foreach (var (movable, transform) in SystemAPI.Query<Movable, LocalTransform>().WithAll<Selected>())
            {
                var mme = SystemAPI.GetComponent<MaterialMeshInfo>(movable.PathEntity);
                var pathing = SystemAPI.GetBuffer<UnitPathing>(movable.PathEntity);
                var mesh = graphicsSystem.GetMesh(mme.MeshID);
                CellMesh.GenerateChunkMesh(pathing.Reinterpret<int2>().AsNativeArray(), mesh);
                SystemAPI.SetComponentEnabled<MaterialMeshInfo>(movable.PathEntity, true);
                SystemAPI.SetComponent(movable.PathEntity, LocalTransform.FromMatrixSafe(math.fastinverse(transform.ToMatrix())));
            }
            
            foreach(var movable in SystemAPI.Query<Movable>().WithNone<Selected>())
                SystemAPI.SetComponentEnabled<MaterialMeshInfo>(movable.PathEntity, false);
        }
    }
}