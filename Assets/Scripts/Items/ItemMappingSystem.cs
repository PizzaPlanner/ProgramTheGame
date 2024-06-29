using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial class ItemMappingSystem : IdentifiableMappingSystem
{
    internal readonly static SharedStatic<NativeParallelHashMap<Identifieable, Item>> ItemsById 
        = SharedStatic<NativeParallelHashMap<Identifieable, Item>>.GetOrCreate<ItemMappingSystem>();

    protected override void OnCreate()
    {
        ItemsById.Data = new NativeParallelHashMap<Identifieable, Item>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        ItemsById.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        foreach (var (item, id) in
                 SystemAPI.Query<Item, Identifieable>()
                          .WithAll<Prefab, PreIdentifiable>()
                          .WithNone<Parent>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            ItemsById.Data[id] = item;
        }

        foreach (var (id, starterInventory) in 
                 SystemAPI.Query<Identifieable, DynamicBuffer<StarterInventory>>()
                          .WithAll<Prefab, PreIdentifiable>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            var handledIds = new NativeList<Identifieable>(Allocator.Temp);
            foreach (var item in starterInventory)
            {
                if(handledIds.Contains(id))
                    continue;
                
                AddRelation(id, new IdentifiableLink{To = item.Type, Relation = "StartsWith"});
                handledIds.Add(id);
            }
        }
    }
}