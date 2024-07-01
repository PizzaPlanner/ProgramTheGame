using Core;
using Identifiables;
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

public partial class LogicParametersMappingSystem : IdentifiableMappingSystem
{
    static readonly SharedStatic<BlobAssetReference<BlobArray<BlobArray<ItemTypeLogicParameter>>>> TypeParameters =
        SharedStatic<BlobAssetReference<BlobArray<BlobArray<ItemTypeLogicParameter>>>>.GetOrCreate<ItemTypeLogicParameter>();
    
    public static NativeList<TypeIndex> AllParameterTypeIndices { get; private set; }

    public static NativeArray<ItemTypeLogicParameter> GetAllParameters(Identifieable id)
    {
        ref var parameters = ref ParametersOf(id);
        var numIds = parameters.Length;
        var array = new NativeArray<ItemTypeLogicParameter>(numIds, Allocator.Temp);
        for (int i = 0; i < numIds; i++)
            array[i] = parameters[i];
        return array;
    }
    
    static ref BlobArray<ItemTypeLogicParameter> ParametersOf(Identifieable id) => ref TypeParameters.Data.Value[id.CurrentIndex];

    public static bool TryGetLogicParameter(Identifieable id, int index, out ItemTypeLogicParameter data)
    {
        data = ParametersOf(id).Length > index ? ParametersOf(id)[index] : default;
        return ParametersOf(id).Length > index;
    } 
    public static bool TryGetLogicParameter(ParameterAccess v, out ItemTypeLogicParameter data) => TryGetLogicParameter(v.TypeId, v.TypeParameterIndex, out data);
    
    protected override void OnCreate()
    {
        AllParameterTypeIndices = new (Allocator.Persistent);
    }

    protected override void OnStartRunning()
    {
#if UNITY_EDITOR
        AllParameterTypeIndices.Clear();
#endif
    }

    protected override void OnDestroy()
    {
        if(TypeParameters.Data.IsCreated)
            TypeParameters.Data.Dispose();
        AllParameterTypeIndices.Dispose();
    }

    // TODO: Make sure all types have sequential layout
    protected override void OnUpdate()
    {
        var idTypeMap = new Dictionary<Identifieable, List<ItemTypeLogicParameter>>(); 
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
        
        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<BlobArray<BlobArray<ItemTypeLogicParameter>>>();
        var idArrayBuilder = blobBuilder.Allocate(ref root, IdentifiableLibrary.Length);
        foreach (var key in idTypeMap)
        {
            var paramBuilder = blobBuilder.Allocate(ref idArrayBuilder[key.Key.CurrentIndex], key.Value.Count);
            for (int i = 0; i < key.Value.Count; i++)
            {
                paramBuilder[i] = key.Value[i];
            }
        }
        if(TypeParameters.Data.IsCreated)
            TypeParameters.Data.Dispose();
        TypeParameters.Data = blobBuilder.CreateBlobAssetReference<BlobArray<BlobArray<ItemTypeLogicParameter>>>(Allocator.Persistent);
    }

    LogicType TypeToLogicType(Type type)
    {
        if(type == typeof(Entity))
            return LogicType.Unit;
        if(type == typeof(Cell))
            return LogicType.Coordinate;
        if(type == typeof(Identifieable))
            return LogicType.Type;
        return LogicType.Number;
    }

    public override VisualElement ProvideWikiDetails(Identifieable id)
    {
        var numParams = TypeParameters.Data.Value[id.CurrentIndex].Length;
        if(numParams == 0)
            return null;
        
        var typeParams = new ItemTypeLogicParameter[numParams];
        for (int i = 0; i < numParams; i++)
            typeParams[i] = ParametersOf(id)[i];
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