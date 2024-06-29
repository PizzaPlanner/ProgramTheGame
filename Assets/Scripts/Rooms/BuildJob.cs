using Identifiables;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

public struct PlannedConstruction : IComponentData
{
    public Entity Parent;
    public Identifieable Prefab;
    public Cell Location;
    public byte Rotation;
    public bool IsGhost => Materials.Length > 0;
    public FixedList64Bytes<BuildMaterials> Materials;
    // public FixedString64Bytes Name;
    
    public LocalTransform ToTransform => LocalTransform.FromPosition(GridStructure.CellCenterToLocal(Location));
}

public partial struct BuildJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<RoomModel> roomLookup;
    [ReadOnly] public ComponentLookup<GridBounds> boundsLookup;
    public ComponentLookup<GridStructure> structureLookup;

    void Execute(Entity e, in PlannedConstruction planned)
    {
        ecb.DestroyEntity(e);
        
        // Get prefab from id
        var prefab = planned.Prefab.AsPrefab;
        if (prefab == Entity.Null || !boundsLookup.TryGetComponent(prefab, out var bounds))
        {
            Debug.LogWarning($"Discarded invalid pending part with ID {planned.Prefab.StableId}");
            return;
        }
        if (!roomLookup.TryGetComponent(planned.Parent, out var Rooms))
        {
            Debug.LogError($"Can not build on {planned.Parent}");
            return;
        }
        
        var area = new GridBounds
        {
            Min = (int2)planned.Location + bounds.Min,
            Max = (int2)planned.Location + bounds.Max,
        };
        
        if (Rooms.IsOccupied(area))
        {
            Debug.LogWarning($"Discarded pending part in invalid or occupied area {bounds.Min}/{bounds.Max}");
            return;
        }

        // Create Part
        var entity = ecb.Instantiate(prefab);
        ecb.SetComponent(entity, planned.ToTransform);
        ecb.AddComponent(entity, new Parent { Value = planned.Parent });

        // if (!planned.Name.IsEmpty)
        // {
        //     ecb.AddComponent(entity, new Name{ Value = planned.Name });
        // }
        //
        // if (planned.Color != default)
        // {
        //     ecb.AddComponent(entity, new URPMaterialPropertyBaseColor{Value = new float4(planned.Color.r, planned.Color.g, planned.Color.b, 1) });
        // }
        Rooms.AddOccupiedArea(area);
        if (planned.IsGhost)
        {
            ecb.AddComponent(entity, new UnderConstruction());
            var inventory = ecb.SetBuffer<Inventory>(entity);
            var pendingOrders = ecb.AddBuffer<PendingItemOrders>(entity);
            foreach (var requirement in planned.Materials)
            {
                var order = ecb.CreateEntity();
                ecb.AddComponent(order, new ItemOrder
                {
                    Amount = requirement.Amount, 
                    Item = requirement.Item,
                    State = OrderState.Requested,
                    Requester = entity
                });
                inventory.Add(new Inventory
                {
                    Transfer = Inventory.TransferDirection.In, 
                    Type = requirement.Item,
                    IsTypeLocked = true,
                });
                pendingOrders.Add(new PendingItemOrders{Order = order});
            }
            
            ecb.AddComponent(entity,
                new FixGhostVisualsAndPhysicsSystem.FixGhostandChildren());
        }
        else
        {
            ecb.AddComponent<PendingGridUpdate>(planned.Parent);
            ecb.RemoveComponent<BuildMaterials>(entity);
            Rooms.AddWall(area);
            var gridBbox = structureLookup[planned.Parent];
            gridBbox.BoundsMin = math.min(gridBbox.BoundsMin, area.Min);
            gridBbox.BoundsMax = math.max(gridBbox.BoundsMax, area.Max);
            structureLookup[planned.Parent] = gridBbox;
        }
    }
}