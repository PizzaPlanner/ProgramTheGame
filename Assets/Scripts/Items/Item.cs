using Identifiables;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;

public struct Item : IDefRef<ItemDef>
{
    public Item(BlobAssetReference<ItemDef> def)
    {
        Def = def;
        Identifiable id;
    }
    public BlobAssetReference<ItemDef> Def { get; private set; }
}

public partial struct ItemDef : IDef
{
    public uint MaxStack;
    public uint MaxValue;
    public uint DefaultValue;
    public bool IsStackable => MaxStack > 1;

    partial class MappingSystem : IdentifiableMappingSystem
    {
        protected override void OnUpdate()
        {
            DefaultMappingUpdate<Item>();
            
            foreach (var (id, starterInventory) in 
                     SystemAPI.Query<Identifiable, DynamicBuffer<InventoryInitializer>>()
                              .WithAll<Prefab>()
                              .WithOptions(EntityQueryOptions.IncludePrefab))
            {
                var handledIds = new NativeHashSet<Identifiable>(4, Allocator.Temp);
                foreach (var item in starterInventory)
                {
                    if(handledIds.Add(id))
                        AddRelation(id, new IdentifiableLink{To = item.Type, Relation = "StartsWith"});
                }
            }
        }
    }
}