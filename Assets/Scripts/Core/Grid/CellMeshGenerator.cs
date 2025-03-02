using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using float2 = Unity.Mathematics.float2;

namespace Grids
{
    public struct TileVisuals : IComponentData
    {
        public UnityObjectRef<Texture2D> Floor;
        public UnityObjectRef<Texture2D> WallSide;
        public UnityObjectRef<Texture2D> WallTop;
    
        public Rect FloorUVs;
        public Rect WallUVs;
        public Rect TopUVs;
        
        public UVSet AsUVs => new UVSet
        {
            BottomMinMax  = new float2x2(FloorUVs.min, FloorUVs.max),
            SideMinMax  = new float2x2(WallUVs.min, WallUVs.max),
            TopMinMax = new float2x2(TopUVs.min, TopUVs.max),
        };
    }

    public struct UVSet
    {
        public float2x2 TopMinMax;
        public float2x2 SideMinMax;
        public float2x2 BottomMinMax;
        
        public float2x4 Top => AsUVs(TopMinMax);
        public float2x4 Side => AsUVs(SideMinMax);
        public float2x4 Bottom => AsUVs(BottomMinMax);

        float2x4 AsUVs(float2x2 rect)
        {
            var bleedTolerance = (rect.c1 - rect.c0) * 0.01f;
            var min = rect.c0 + bleedTolerance;
            var max = rect.c1 - bleedTolerance;
            return new float2x4(
                new float2(min.x, min.y),
                new float2(max.x, min.y),
                new float2(max.x, max.y),
                new float2(min.x, max.y)  
            );
        }
    }

    
    public static class CellMesh
    {

        public static Mesh GenerateTopologyMeshWithUVs(CGrid grid, Mesh mesh, 
                                                       ComponentLookup<TileVisuals> tileLookup, 
                                                       ComponentLookup<BakedIntoRoomMap> bakedLookup)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
        
            var bounds = grid.BoundsWithBorder;
            var voxelMap = new NativeHashSet<int3>(bounds.Size, Allocator.Temp);
            var tileMap = new NativeHashMap<int3, UVSet>(bounds.Size, Allocator.Temp);
            foreach(var e in grid.GetAllEntities())
                if (tileLookup.TryGetComponent(e, out var tile) && bakedLookup.TryGetComponent(e, out var baked))
                {
                    var loc = new int3(baked.Offset.x, baked.Layer.IsWall()? 1 : 0, baked.Offset.y);
                    voxelMap.Add(loc);
                    tileMap.Add(loc, tile.AsUVs);
                }
                
            foreach(var voxel in voxelMap)
                AddCube(vertices, triangles, uvs, voxel, tileMap[voxel], voxelMap);

            UpdateMesh(ref mesh, vertices, triangles, uvs);
            return mesh;
        }
        
        public static Mesh GenerateChunkMesh(CGrid grid, Mesh mesh = null)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
            
            var uvSet = new UVSet
            {
                BottomMinMax = new float2x2(0, 0.25f),
                SideMinMax = new float2x2(new float2(0.5f, 0), new float2(1f, 0.5f)),
                TopMinMax = new float2x2(new float2(0.25f, 0.5f), new float2(0.5f, 0.75f)),
            };
        
            var bounds = grid.BoundsWithBorder;
            var voxelMap = new NativeHashSet<int3>(bounds.Size, Allocator.Temp);
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if((grid.GetPathing(new int2(x, y)) & BuildingLayer.FloorLevel) != 0)
                    voxelMap.Add(new int3(x, 1, y));
            }
            foreach(var voxel in voxelMap)
                AddCube(vertices, triangles, uvs, voxel, uvSet, voxelMap);

            UpdateMesh(ref mesh, vertices, triangles, uvs);
            return mesh;
        }
        
        public static Mesh GenerateChunkMesh(NativeArray<int2> cells, Mesh mesh = null)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
            
            var uvSet = new UVSet
            {
                BottomMinMax = new float2x2(0, 0.25f),
                SideMinMax = new float2x2(new float2(0.5f, 0), new float2(1f, 0.5f)),
                TopMinMax = new float2x2(new float2(0.25f, 0.5f), new float2(0.5f, 0.75f)),
            };
            
            var voxelMap = new NativeHashSet<int3>(cells.Length, Allocator.Temp);
            
            foreach(var c in cells)
                voxelMap.Add(new int3(c.x, 1, c.y));
            foreach(var voxel in voxelMap)
                AddCube(vertices, triangles, uvs, voxel, uvSet, voxelMap);

            UpdateMesh(ref mesh, vertices, triangles, uvs);
            return mesh;
        }

        static void UpdateMesh(ref Mesh mesh, NativeList<float3> vertices, NativeList<int> triangles, NativeList<float2> uvs)
        {
            mesh ??= new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices.ToArray(Allocator.Temp));
            mesh.triangles = triangles.ToArrayNBC();
            mesh.SetUVs(0, uvs.ToArray(Allocator.Temp));
            mesh.Optimize();
            mesh.RecalculateNormals();
        }
        
        static void AddCube(NativeList<float3> vertices, NativeList<int> triangles, NativeList<float2> uvs, int3 pos, UVSet selfUVs, NativeHashSet<int3> voxelMap)
        {
            float3 min = new float3(0, -1, 0);
            float3 max = new float3(1, pos.y, 1);
            float3[] faceOffsets = new float3[]
            {
                new (min.x, min.y, min.z), new (max.x, min.y, min.z),
                new (max.x, max.y, min.z), new (min.x, max.y, min.z), // Back
                new (min.x, min.y, max.z), new (max.x, min.y, max.z),
                new (max.x, max.y, max.z), new (min.x, max.y, max.z)  // Front
            };

            int[][] faces = new int[][]
            {
                new int[] {0, 1, 2, 3}, // Back
                new int[] {5, 4, 7, 6}, // Front
                new int[] {4, 0, 3, 7}, // Left
                new int[] {1, 5, 6, 2}, // Right
                new int[] {3, 2, 6, 7}, // Top
                new int[] {4, 5, 1, 0}  // Bottom
            };

            bool[] shouldRender = new bool[]
            {
                !voxelMap.Contains(pos + (int3)math.back()),
                !voxelMap.Contains(pos + (int3)math.forward()),
                !voxelMap.Contains(pos + (int3)math.left()),
                !voxelMap.Contains(pos + (int3)math.right()),
                true,
                true
            };

            for (int face = 0; face < 6; face++)
            {
                if (!shouldRender[face]) continue;

                int vertexIndex = vertices.Length;
                float2x4 selectedUVs = (face == 4) ? selfUVs.Top : (face == 5) ? selfUVs.Bottom : selfUVs.Side;

                for (int v = 0; v < 4; v++)
                {
                    vertices.Add(pos + faceOffsets[faces[face][v]]);
                    uvs.Add(selectedUVs[v]);
                }

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 2);
            }
        }
    }
}