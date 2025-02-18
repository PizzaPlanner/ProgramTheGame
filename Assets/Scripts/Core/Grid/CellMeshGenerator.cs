using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using float2 = Unity.Mathematics.float2;

namespace Grids
{
    public static class CellMesh
    {
            
        public static Mesh GenerateChunkMesh(CGrid grid, Mesh mesh = null)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
        
            var bounds = grid.BoundsWithBorder;
            var voxelMap = new NativeHashSet<int2>(bounds.Size, Allocator.Temp);
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if((grid.GetPathing(new int2(x, y)) & BuildingLayer.FloorLevel) != 0)
                    voxelMap.Add(new int2(x, y));
            }
            foreach(var voxel in voxelMap)
                AddCube(vertices, triangles, uvs, new int3(voxel.x, 0, voxel.y), voxelMap);

            UpdateMesh(ref mesh, vertices, triangles, uvs);
            return mesh;
        }
        
        public static Mesh GenerateChunkMesh(NativeArray<int2> cells, Mesh mesh = null)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
            
            var voxelMap = new NativeHashSet<int2>(cells.Length, Allocator.Temp);
            
            foreach(var c in cells)
                voxelMap.Add(c);
            foreach(var c in cells)
                AddCube(vertices, triangles, uvs, new int3(c.x, 0, c.y), voxelMap);

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
        
        static void AddCube(NativeList<float3> vertices, NativeList<int> triangles, NativeList<float2> uvs, int3 pos, NativeHashSet<int2> voxelMap)
        {
            float3 min = new float3(0, 0, 0);
            float3 max = new float3(1, 2, 1);
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

            var topUVs = new float2[]
            {
                new float2(0, 0.5f), new float2(0.25f, 0.5f), new float2(0.25f, 0.75f), new float2(0, 0.75f)
            };
            var sideUVs = new float2[]
            {
                new float2(0.5f, 0), new float2(1, 0), new float2(1, 0.5f), new float2(0.5f, 0.5f)
            };
            var bottomUVs = new float2[]
            {
                new float2(0, 0), new float2(0.25f, 0), new float2(0.25f, 0.25f), new float2(0, 0.25f)
            };

            bool[] shouldRender = new bool[]
            {
                !voxelMap.Contains(pos.xz + (int2)math.back().xz),
                !voxelMap.Contains(pos.xz + (int2)math.forward().xz),
                !voxelMap.Contains(pos.xz + (int2)math.left().xz),
                !voxelMap.Contains(pos.xz + (int2)math.right().xz),
                true,
                true
            };

            for (int face = 0; face < 6; face++)
            {
                if (!shouldRender[face]) continue;

                int vertexIndex = vertices.Length;
                float2[] selectedUVs = (face == 4) ? topUVs : (face == 5) ? bottomUVs : sideUVs;

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