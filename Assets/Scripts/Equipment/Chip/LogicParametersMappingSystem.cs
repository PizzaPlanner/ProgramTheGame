using Core;
using Identifiables;
using Identifiables.UI;
using Programmable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Burst;
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

public partial class LogicParametersMappingSystem : IdentifiableMappingSystem, IWikiProvider
{
    // static readonly SharedStatic<BlobAssetReference<BlobArray<BlobArray<ItemTypeLogicParameter>>>> TypeParameters =
    //     SharedStatic<BlobAssetReference<BlobArray<BlobArray<ItemTypeLogicParameter>>>>.GetOrCreate<ItemTypeLogicParameter>();
    
    static readonly SharedStatic<UnsafeHashMap<Identifiable, NativeArray<ItemTypeLogicParameter>>> TypeParameters2 =
        SharedStatic<UnsafeHashMap<Identifiable, NativeArray<ItemTypeLogicParameter>>>.GetOrCreate<ItemTypeLogicParameter>();
    
    public static NativeList<TypeIndex> AllParameterTypeIndices { get; private set; }
    
    public static NativeArray<ItemTypeLogicParameter> ParametersOf(Identifiable id) => TypeParameters2.Data.TryGetValue(id, out var array) ? array 
        : new NativeArray<ItemTypeLogicParameter>(0, Allocator.Temp);

    public static bool TryGetLogicParameter(Identifiable id, int index, out ItemTypeLogicParameter data)
    {
        data = ParametersOf(id).Length > index ? ParametersOf(id)[index] : default;
        return ParametersOf(id).Length > index;
    } 
    public static bool TryGetLogicParameter(ParameterAccess v, out ItemTypeLogicParameter data) => TryGetLogicParameter(v.TypeId, v.TypeParameterIndex, out data);
    
    protected override void OnCreate()
    {
        AllParameterTypeIndices = new (Allocator.Persistent);
        TypeParameters2.Data = new(0, Allocator.Persistent);
    }

    protected override void OnStartRunning()
    {
#if UNITY_EDITOR
        AllParameterTypeIndices.Clear();
#endif
    }

    protected override void OnDestroy()
    {
        if (TypeParameters2.Data.IsCreated)
        {
            foreach(var array in TypeParameters2.Data.GetValueArray(Allocator.Temp))
                array.Dispose();
            TypeParameters2.Data.Dispose();
        }
        AllParameterTypeIndices.Dispose();
    }

    // TODO: Make sure all types have sequential layout
    protected override void OnUpdate()
    {
        var idTypeMap = new Dictionary<Identifiable, List<ItemTypeLogicParameter>>(); 
        foreach (var (id, e) in
                 SystemAPI.Query<Identifiable>()
                          .WithAll<Prefab>()
                          .WithNone<IdentifiablesMappingGroup.Mapped>()
                          .WithOptions(EntityQueryOptions.IncludePrefab)
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
            var typeParams = new List<ItemTypeLogicParameter>();
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
                    typeParams.Add(new ItemTypeLogicParameter
                    {
                        Name = field.Name,
                        Access = field.Item2 ? LogicAccessType.ReadWrite : LogicAccessType.Read,
                        Offset = bytes,
                        TypeIndex = typeIndex,
                        TypeSizeInBytes = fieldSize,
                        Type = TypeToLogicType(field.FieldType)
                    });
                    bytes += fieldSize;
                    i++;
                }
            }
            if(typeParams.Count > 0)
                idTypeMap.Add(id, typeParams);
        }
        
        foreach (var key in idTypeMap)
        {
            var array = new NativeArray<ItemTypeLogicParameter>(key.Value.Count, Allocator.Persistent);
            for (int i = 0; i < key.Value.Count; i++)
                array[i] = key.Value[i];
            TypeParameters2.Data[key.Key] = array;
        }
    }

    LogicType TypeToLogicType(Type type)
    {
        if(type == typeof(Entity))
            return LogicType.Unit;
        if(type == typeof(Cell))
            return LogicType.Coordinate;
        if(type == typeof(Identifiable))
            return LogicType.Type;
        return LogicType.Number;
    }

    public VisualElement ProvideWikiDetails(Identifiable id)
    {
        var typeParams = ParametersOf(id);
        if(typeParams.Length == 0)
            return null;
        
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
        mc.itemsSource = typeParams.ToArray();
        ve.Add(mc);
        return ve;
    }
}