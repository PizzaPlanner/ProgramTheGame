using Identifiables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial class ItemMappingSystem : IdentifiableMappingSystem
{
    protected override void OnCreate()
    {
        ItemId.ItemsById.Data = new NativeParallelHashMap<Identifiable, Item>(0, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        ItemId.ItemsById.Data.Dispose();
    }

    protected override void OnUpdate()
    {
        foreach (var (item, id) in
                 SystemAPI.Query<Item, Identifiable>()
                          .WithAll<Prefab>()
                          .WithNone<Parent, IdentifiablesMappingGroup.Mapped>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            ItemId.ItemsById.Data[id] = item;
        }

        foreach (var (id, starterInventory) in 
                 SystemAPI.Query<Identifiable, DynamicBuffer<StarterInventory>>()
                          .WithAll<Prefab>()
                          .WithNone<IdentifiablesMappingGroup.Mapped>()
                          .WithOptions(EntityQueryOptions.IncludePrefab))
        {
            var handledIds = new NativeList<Identifiable>(Allocator.Temp);
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