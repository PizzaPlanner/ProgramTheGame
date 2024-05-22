using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

public struct ItemTypeLogicParameter
{
    public int Offset;
    public FixedString32Bytes Name;
    public LogicAccessType Access;

    public override string ToString()
    {
        return $"{Name} ({Access})";
    }
}

public struct TypeLogicLinkData
{
    public int Offset;
    public int Length;
}

partial class ItemTypeLogicParametersMappingSystem : IdentifiableMappingSystem
{ 
    static NativeParallelHashMap<Identifieable, int> TypeNumParameters;
    static NativeParallelHashMap<(Identifieable, int), ItemTypeLogicParameter> TypeParameters;
    static NativeParallelHashMap<(Identifieable, TypeIndex), TypeLogicLinkData> TypeOffsets;

    public static NativeArray<ItemTypeLogicParameter> GetAllParameters(Identifieable id)
    {
        var numIds = TypeNumParameters.TryGetValue(id, out var n) ? n : 0;
        var array = new NativeArray<ItemTypeLogicParameter>(numIds, Allocator.Temp);
        for (int i = 0; i < numIds; i++)
            array[i] = TypeParameters[(id, i)];
        return array;
    }
    
    public static bool TryGetLogicParameter(Identifieable id, int index, out ItemTypeLogicParameter data) => TypeParameters.TryGetValue((id, index), out data);
    public static TypeLogicLinkData GetAccessibleIndexFor(Identifieable id, TypeIndex type) => TypeOffsets.TryGetValue((id, type), out var data) ? data : default;
    public static TypeLogicLinkData GetAccessibleIndexFor<T>(Identifieable id) => TypeOffsets.TryGetValue((id, TypeManager.GetTypeIndex<T>()), out var data) ? data : default;
    
    protected override void OnCreate()
    {
        TypeParameters = new NativeParallelHashMap<(Identifieable, int), ItemTypeLogicParameter>(0, Allocator.Persistent);
        TypeNumParameters = new NativeParallelHashMap<Identifieable, int>(0, Allocator.Persistent);
        TypeOffsets = new NativeParallelHashMap<(Identifieable, TypeIndex), TypeLogicLinkData>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        TypeNumParameters.Dispose();
        TypeParameters.Dispose();
        TypeOffsets.Dispose();
    }

    protected override void OnUpdate()
    {
        // var getComponentDataBaseMethod = typeof(EntityManager)
        //                                  .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        //                                 .First(x => x.Name == "GetComponentData" 
        //                                             && x.GetParameters().Length == 1 
        //                                             && x.GetParameters()[0].ParameterType == typeof(Entity));
        // Assert.IsNotNull(getComponentDataBaseMethod);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (id, e) in
                 SystemAPI.Query<Identifieable>()
                          .WithAll<Prefab, PreIdentifiable>()
                          .WithNone<ChipAccessibleData>()
                          .WithEntityAccess())
        {
            var chipTypes = new List<(Type Data, Type Component)>(); 
            foreach (var compType in EntityManager.GetChunk(e).Archetype.GetComponentTypes())
            {
                var targetTypes = compType.GetManagedType().GetInterfaces()
                                          .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IChipTargetableImpl<>))
                                          .Select(x => (x.GetGenericArguments()[0], compType.GetManagedType())).ToArray();
                if(targetTypes.Any())
                    chipTypes.Add(targetTypes.First());
            }
            
            if(!chipTypes.Any())
                continue;

            int i = 0;
            var bytes = 0;
            var buffer = ecb.AddBuffer<ChipAccessibleData>(e);
            foreach (var (type, compType) in chipTypes)
            {
                var logicFields = type.GetFields()
                                      .OrderBy(field => field.MetadataToken)
                                      .Select(x => (x.Name, 
                                          x.GetCustomAttribute<LogicWritableAttribute>() != null,
                                          UnsafeUtility.SizeOf(x.FieldType)))
                                      .ToArray();

                var typeData = new TypeLogicLinkData()
                {
                    Offset = bytes,
                };
                foreach (var field in logicFields)
                {
                    
                    TypeParameters.Add((id, i++), new ItemTypeLogicParameter
                    {
                        Name = field.Name,
                        Access = field.Item2 ? LogicAccessType.ReadWrite : LogicAccessType.Read,
                        Offset = bytes
                    });
                    bytes += field.Item3;
                }
                typeData.Length = bytes - typeData.Offset;
                TypeOffsets.Add((id, TypeManager.GetTypeIndex(type)), typeData);
            }
            TypeNumParameters.Add(id, i);
            buffer.Resize(bytes, NativeArrayOptions.ClearMemory);
        }
        ecb.Playback(EntityManager);
    }
}