using Core;
using Programmable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

public enum LogicType
{
    Number,
    Coordinate,
    Type,
    Unit
}

public struct ItemTypeLogicParameter
{
    public int TypeSizeInBytes;
    public int Offset;
    public TypeIndex TypeIndex;
    public FixedString32Bytes Name;
    public LogicAccessType Access;
    public LogicType Type;

    public override string ToString()
    {
        return $"{Name} ({Access})";
    }
}

public partial class LogicParametersMappingSystem : IdentifiableMappingSystem
{ 
    static NativeParallelHashMap<Identifieable, int> TypeNumParameters;
    static NativeParallelHashMap<(Identifieable, int), ItemTypeLogicParameter> TypeParameters;
    public static NativeList<TypeIndex> AllParameterTypeIndices { get; private set; }

    public static NativeArray<ItemTypeLogicParameter> GetAllParameters(Identifieable id)
    {
        var numIds = TypeNumParameters.TryGetValue(id, out var n) ? n : 0;
        var array = new NativeArray<ItemTypeLogicParameter>(numIds, Allocator.Temp);
        for (int i = 0; i < numIds; i++)
            array[i] = TypeParameters[(id, i)];
        return array;
    }
    
    public static bool TryGetLogicParameter(Identifieable id, int index, out ItemTypeLogicParameter data) => TypeParameters.TryGetValue((id, index), out data);
    public static bool TryGetLogicParameter(ParameterAccess v, out ItemTypeLogicParameter data) => TypeParameters.TryGetValue((v.TypeId, v.TypeParameterIndex), out data);
    protected override void OnCreate()
    {
        TypeParameters = new NativeParallelHashMap<(Identifieable, int), ItemTypeLogicParameter>(0, Allocator.Persistent);
        TypeNumParameters = new NativeParallelHashMap<Identifieable, int>(0, Allocator.Persistent);
        AllParameterTypeIndices = new NativeList<TypeIndex>(Allocator.Persistent);
    }

    protected override void OnStartRunning()
    {
#if UNITY_EDITOR
        TypeParameters.Clear();
        TypeNumParameters.Clear();
        AllParameterTypeIndices.Clear();
#endif
    }

    protected override void OnDestroy()
    {
        TypeNumParameters.Dispose();
        TypeParameters.Dispose();
        AllParameterTypeIndices.Dispose();
    }

    // TODO: Make sure all types have sequential layout
    protected override void OnUpdate()
    {
        var query = SystemAPI.QueryBuilder().WithAll<PreIdentifiable, Prefab, Identifieable>()
                             .WithOptions(EntityQueryOptions.IncludePrefab)
                             .WithNone<IdForChip>().Build();
        EntityManager.AddComponent<IdForChip>(query);
        foreach (var (id, chipId) in SystemAPI.Query<Identifieable, RefRW<IdForChip>>())
        {
            chipId.ValueRW.Id = id;
        }
        
        foreach (var (id, e) in
                 SystemAPI.Query<Identifieable>()
                          .WithAll<Prefab, PreIdentifiable>()
                          .WithEntityAccess())
        {
            var chipTypes = new List<Type>(); 
            foreach (var compType in EntityManager.GetChunk(e).Archetype.GetComponentTypes())
            {
                // var targetTypes = compType.GetManagedType().GetInterfaces()
                //                           .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IChipTargetableImpl<>))
                //                           .Select(x => (x.GetGenericArguments()[0], compType.GetManagedType())).ToArray();
                
                if(typeof(IChipTargetable).IsAssignableFrom(compType.GetManagedType()))
                    chipTypes.Add(compType.GetManagedType());
            }
            
            if(!chipTypes.Any())
                continue;

            int i = 0;
            foreach (var type in chipTypes)
            {
                var typeIndex = TypeManager.GetTypeIndex(type);
                if(!AllParameterTypeIndices.Contains(typeIndex))
                    AllParameterTypeIndices.Add(typeIndex);
                
                var logicFields = type.GetFields()
                                      .OrderBy(field => field.MetadataToken)
                                      .Select(x => (x.Name, 
                                          x.GetCustomAttribute<LogicWritableAttribute>() != null,
                                          x.FieldType))
                                      .ToArray();
                
                var bytes = 0;
                foreach (var field in logicFields)
                {
                    var fieldSize = UnsafeUtility.SizeOf(field.FieldType);
                    if(!TypeParameters.TryAdd((id, i), new ItemTypeLogicParameter
                    {
                        Name = field.Name,
                        Access = field.Item2 ? LogicAccessType.ReadWrite : LogicAccessType.Read,
                        Offset = bytes,
                        TypeIndex = typeIndex,
                        TypeSizeInBytes = fieldSize,
                        Type = TypeToLogicType(field.FieldType)
                    }))
                        Debug.Log($"Tried to add {id} #{i} a second time.");
                    bytes += fieldSize;
                    i++;
                }
            }
            TypeNumParameters.Add(id, i);
        }
        
    }

    LogicType TypeToLogicType(Type type)
    {
        if(type == typeof(Entity))
            return LogicType.Unit;
        if(type == typeof(Coordinate))
            return LogicType.Coordinate;
        if(type == typeof(Identifieable))
            return LogicType.Type;
        return LogicType.Number;
    }

    public override VisualElement ProvideWikiDetails(Identifieable id)
    {
        if(!TypeNumParameters.TryGetValue(id, out var numParams))
            return null;
        var typeParams = new ItemTypeLogicParameter[numParams];
        for (int i = 0; i < numParams; i++)
            typeParams[i] = TypeParameters[(id, i)];
        var ve = new Foldout(){text = "Logic Parameters"};
        
        var mc = new MultiColumnListView();

        var cid = new Column(){title = "Id", resizable = false};
        cid.makeCell = () => new Label();
        cid.bindCell = (element, i) => { (element as Label).text = i.ToString(); };
        mc.columns.Add(cid);
        
        var name = new Column(){title = "Name", resizable = false, minWidth = 150};
        name.makeCell = () => new Label();
        name.bindCell = (element, i) => { (element as Label).text = typeParams[i].Name.Value; };
        mc.columns.Add(name);
        
        var type = new Column(){title = "Type", resizable = false, minWidth = 100};
        type.makeCell = () => new Label();
        type.bindCell = (element, i) => { (element as Label).text = typeParams[i].Type.ToString(); };
        mc.columns.Add(type);
        
        var access = new Column(){title = "Access", resizable = false, minWidth = 100};
        access.makeCell = () => new Label();
        access.bindCell = (element, i) => { (element as Label).text = typeParams[i].Access.ToString(); };
        mc.columns.Add(access);
        
        var description = new Column(){title = "Description", resizable = false, stretchable = true};
        description.makeCell = () => new Label();
        description.bindCell = (element, i) => { (element as Label).text = $"logic.desc.{id}.{typeParams[i].Name}"; };
        mc.columns.Add(description);
        
        mc.columns.stretchMode = Columns.StretchMode.GrowAndFill;
        mc.columns.resizable = false;
        mc.columns.reorderable = false;
        mc.fixedItemHeight = 32;
        mc.itemsSource = typeParams;
        ve.Add(mc);
        return ve;
    }
}