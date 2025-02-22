using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

partial class MapGenerationMappingSystem : IdentifiableMappingSystem
{
    protected override void OnDestroy()
    {
        if(NodeSerializer.functionPointers.IsCreated)
            NodeSerializer.functionPointers.Dispose();
    }

    protected override void OnUpdate()
    {
        OnDestroy();
        NodeSerializer.functionPointers = new NativeHashMap<ulong, FunctionPointer<NodeSerializer.ExecuteDelegate>>(0, Allocator.Persistent);
        foreach (var node in SystemAPI.Query<NodeSerializer>().WithOptions(EntityQueryOptions.IncludePrefab))
        {
            if(NodeSerializer.functionPointers.TryGetValue(node.TypeHash, out _))
                continue;
            var type = TypeManager.GetTypeIndexFromStableTypeHash(node.TypeHash);
            if (type == TypeIndex.Null)
            {
                Debug.LogError($"No TypeIndex associated with hash {node.TypeHash}");
                continue;
            }
            var method = TypeManager.GetType(type).GetMethod("ExecuteFrom", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Debug.LogError($"Type {type} is missing ExecuteFrom method");
                continue;
            }
            var func = (NodeSerializer.ExecuteDelegate)method.CreateDelegate(typeof(NodeSerializer.ExecuteDelegate));
            NodeSerializer.functionPointers.Add(node.TypeHash, BurstCompiler.CompileFunctionPointer(func));
        }
    }
}