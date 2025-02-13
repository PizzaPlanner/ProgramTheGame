using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Mathematics;
using UnityEngine;

namespace Grids
{
    public static class CellMesh
    {
            
        public static Mesh GenerateChunkMesh(CGrid grid, Mesh mesh = null)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
        
            int vertexOffset = 0;
            var bounds = grid.BoundsWithBorder;
            for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
            for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
            {
                if((grid.GetPathing(new int2(x, y)) & BuildingLayer.FloorLevel) == 0)
                    continue;
                AddCubeMesh(vertices, triangles, uvs, new int3(x, 0, y), ref vertexOffset);
            }

            mesh ??= new Mesh();
            mesh.SetVertices(vertices.ToArray(Allocator.Temp));
            mesh.SetUVs(0, uvs.ToArray(Allocator.Temp));
            mesh.triangles = triangles.ToArrayNBC();
            mesh.Optimize();
            mesh.RecalculateNormals();
            return mesh;
        }
        
        public static Mesh GenerateChunkMesh(NativeArray<int2> cells, Mesh mesh = null)
        {
            var vertices = new NativeList<float3>(Allocator.Temp);
            var uvs = new NativeList<float2>(Allocator.Temp);
            var triangles = new NativeList<int>(Allocator.Temp);
            
            int vertexOffset = 0;
            foreach(var c in cells)
                AddCubeMesh(vertices, triangles, uvs, new int3(c.x, 0, c.y), ref vertexOffset);

            mesh ??= new Mesh();
            mesh.SetVertices(vertices.ToArray(Allocator.Temp));
            mesh.SetUVs(0, uvs.ToArray(Allocator.Temp));
            mesh.triangles = triangles.ToArrayNBC();
            mesh.Optimize();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void AddCubeMesh(NativeList<float3> vertices, NativeList<int> triangles, NativeList<float2> uvs, int3 pos, ref int vertexOffset)
        {
            for (int i = 0; i < 8; i++)
            {
                vertices.Add(CubeVertices[i] + pos);
                uvs.Add(CubeUVs[i]);
            }

            for (int i = 0; i < 36; i++)
                triangles.Add(CubeTriangles[i] + vertexOffset);

            vertexOffset += 8;
        }
        
        static readonly float3[] CubeVertices = {
            new float3(0.0f, -1.0f,  0.0f), new float3(1.0f, -1.0f,  0.0f),
            new float3(1.0f,  0.0f,  0.0f), new float3(0.0f,  0.0f,  0.0f),
            new float3(0.0f, -1.0f,  1.0f), new float3(1.0f, -1.0f,  1.0f),
            new float3(1.0f,  0.0f,  1.0f), new float3(0.0f,  0.0f,  1.0f)
        };
        
        static readonly float2[] CubeUVs = {
            new float2(0, 0), new float2(1, 0),
            new float2(1, 1), new float2(0, 1),
            new float2(0, 0), new float2(1, 0),
            new float2(1, 1), new float2(0, 1)
        };
        
        static readonly int[] CubeTriangles = {
            0, 2, 1, 0, 3, 2, // Front
            4, 5, 6, 4, 6, 7, // Back
            0, 1, 5, 0, 5, 4, // Bottom
            2, 3, 7, 2, 7, 6, // Top
            0, 4, 7, 0, 7, 3, // Left
            1, 2, 6, 1, 6, 5  // Right
        };
    }
}