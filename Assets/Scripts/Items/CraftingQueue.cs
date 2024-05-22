using System;
using Unity.Collections;
using Unity.Entities;

public readonly partial struct CrafterAspect : IAspect
{
    [Optional] public readonly RefRO<CraftInProgress> Progress;
    public readonly RefRO<Crafter> Crafter;
    public readonly DynamicBuffer<CraftingQueue> Queue;
    public readonly DynamicBuffer<Craftables> Recipes;
}

public struct CraftingQueue : IBufferElementData
{
    public Entity Recipe;
    public float TimeLeft;
}

public partial struct CraftInProgress : IComponentData
{
    public Entity Transaction;
}

[UpdateAfter(typeof(ItemTransactionSystem))]
public partial class CraftingSystem : SystemBase
{
    partial struct CraftJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<CraftingInput> iBuffers;
        [ReadOnly] public BufferLookup<CraftingOutput> oBuffers;
        public ComponentLookup<ItemTransaction.TransactionHeader> transactionLookup;
        public EntityCommandBuffer ecb;
        public float deltaTime;

        void Execute(Entity e, CrafterAspect crafter)
        {
            if (crafter.Queue.Length == 0)
                return;

            var queue = crafter.Queue;
            var current = queue[0];
            if (crafter.Progress.IsValid) 
            {
                if (!transactionLookup.TryGetComponent(crafter.Progress.ValueRO.Transaction, out var t) || !t.Confirmed)
                {
                    queue.RemoveAt(0);
                    ecb.RemoveComponent<CraftInProgress>(e);
                    return;
                }
                
                current.TimeLeft -= deltaTime;
                queue[0] = current;
                if (current.TimeLeft > 0) 
                    return;
                
                queue.RemoveAt(0);
                t.OnHold = false;
                transactionLookup[crafter.Progress.ValueRO.Transaction] = t;
                ecb.RemoveComponent<CraftInProgress>(e);
                return;
            }
            
            var transaction = ItemTransaction.Builder.Schedule(ecb);
            foreach (var requirement in iBuffers[current.Recipe])
                transaction = transaction.RemoveFrom(crafter.Crafter.ValueRO.InputInventory, requirement.ItemId, requirement.Amount);
            foreach (var output in oBuffers[current.Recipe])
                if(output.ItemId.Category == ItemCategory.Unit)
                    transaction = transaction.AddAndSpawn(crafter.Crafter.ValueRO.OutputInventory, output.ItemId);
                else
                    transaction = transaction.AddTo(crafter.Crafter.ValueRO.OutputInventory, output.ItemId, output.Amount);
            var result = transaction.CommitAndCompleteManually();
            ecb.AddComponent(e, new CraftInProgress{ Transaction = result });
        }
    }
    protected override void OnCreate()
    {
        RequireForUpdate<IdentifiablesMappingGroup.Loaded>();
    }

    protected override void OnUpdate()
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(World.Unmanaged);
        var deltaTime = World.Time.DeltaTime;
        var iBuffers = GetBufferLookup<CraftingInput>(true);
        var oBuffers = GetBufferLookup<CraftingOutput>(true);
        var transactionLookup = SystemAPI.GetComponentLookup<ItemTransaction.TransactionHeader>(false);
        
        Dependency = new CraftJob
        {
            ecb = ecb,
            iBuffers = iBuffers,
            oBuffers = oBuffers,
            deltaTime = deltaTime,
            transactionLookup = transactionLookup
        }.Schedule(Dependency);
    }
}
