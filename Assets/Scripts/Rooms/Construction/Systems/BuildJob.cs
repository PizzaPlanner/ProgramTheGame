using Grids;
using Identifiables;
using Navigation;
using Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

[BurstCompile]
public partial struct BuildJob : IJobEntity
{
    static readonly Identifiable ConstructTaskPrefab = Identifiable.FromName("BuildTask");
    static readonly Identifiable OrderTaskPrefab = Identifiable.FromName("ItemOrderTask");
    
    public EntityCommandBuffer ecb;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<CGrid> gridLookup;
    [ReadOnly] public ComponentLookup<Ghost> ghostLookup;
    
    [BurstCompile]
    void Execute(Entity e, in PlannedConstruction planned)
    {
        // Parent not ready
        if (!gridLookup.TryGetComponent(planned.Parent, out var navigation))
            return;
        
        ecb.DestroyEntity(e);
        
        var prefab = planned.Prefab.SpawnableDef();
        if (prefab == Entity.Null)
        {
            Debug.LogWarning($"Discarded invalid pending part with ID {planned.Prefab.DebugId}");
            return;
        }
        
        if (!BuildingDef.Id.Has(planned.Prefab))
        {
            Debug.Log($"Discarded pending part that is not a building");
            return;
        }
        var building = planned.Prefab.BuildingDef();
        
        var rotation = building.IsRotatable ? planned.Rotation : (byte)0;
        var area = building.ToArea(rotation, planned.Location);
        var isPlacementOk = navigation.IsBuildable(area, building.Layer);
        if (!isPlacementOk && planned.Mode != ConstructionMode.PlacementPreview)
        {
            Debug.LogWarning($"Discarded pending part in invalid or occupied area {area}");
            return;
        }
        
        var isPlacing = planned.Mode == ConstructionMode.PlacementPreview || ghostLookup.HasComponent(planned.Parent);
        var isUnderConstruction = planned.Mode == ConstructionMode.UnderConstruction;
        var baked = new BakedIntoRoomMap
        {
            Rotation = rotation,
            Area = area,
            Layer = building.Layer,
            Grid = planned.Parent,
            Offset = planned.Location,
            IsGhost = isPlacing || isUnderConstruction
        };

        // Create Part
        var entity = ecb.Instantiate(prefab);
        ecb.SetComponent(entity, building.ToTransform(rotation, planned.Location));
        if (isPlacing)
        {
            ecb.AddComponent(entity, new CurrentGhost { ValuesToBake = baked });
            ecb.AddComponent(entity, new Ghost { GhostBuildable = isPlacementOk });
            ecb.SetComponent(entity, planned.Materials);
            ecb.AddComponent(entity, new Parent { Value = planned.Parent });
            ecb.AddComponent(entity, new OnGrid { Grid = planned.Parent });
        }
        else if (isUnderConstruction)
        {
            CreateBuildTasks(planned, entity, building);
            ecb.AddComponent(entity, new Ghost { GhostBuildable = true });
            ecb.SetComponent(entity, planned.Materials);
            ecb.AddComponent(entity,  new ToBeBakedIntoRoomMap { Value = baked });
        }
        else
        {
            ecb.RemoveComponent<ConstructionVariant>(entity);
            if(planned.Materials.PrimaryMass.ElementType != default(ElementDef.Id))
                ecb.SetComponent(entity, planned.Materials.PrimaryMass);
            ecb.AddComponent(entity, new ToBeBakedIntoRoomMap { Value = baked });
        }
    }

    void CreateBuildTasks(PlannedConstruction planned, Entity entity, BuildingDef building)
    {
        ecb.AddBuffer<TaskProvider>(entity);
        var constructTask = Task.Schedule(ConstructTaskPrefab, ecb, new Task
        {
            Target = entity, 
            Priority = TaskPriority.MediumPriority,
            Origin = entity,
            CrewProvider = planned.Parent
        });
        ecb.SetComponent(constructTask, new TaskProgress
        {
            RequiredTicks = building.Duration
        });
        ecb.AddComponent(constructTask, new Parent { Value = entity });
        ecb.SetComponent(constructTask, building.ToTransform(0, planned.Location).Translate(new float3(0.5f, 0, 0.5f) * CGrid.CELL_SIZE));
        ecb.AddComponent(entity, new UnderConstruction { });
        foreach (var requirement in planned.Materials.BuildMaterials)
        {
            if(requirement.Item == default)
                continue;
            ecb.AppendToBuffer(constructTask, new Inventory() 
            {
                Transfer = Inventory.TransferDirection.In, 
                IsTypeLocked = true, 
                Type = requirement.Item
            });
            var order = Task.Schedule(OrderTaskPrefab, ecb, new Task
            {
                Priority = TaskPriority.MediumPriority,
                Origin = constructTask,
                CrewProvider = planned.Parent
            });
            ecb.SetComponent(order, new ItemOrderTask
            {
                Amount = (ushort)requirement.Amount, 
                Item = requirement.Item,
                Requester = constructTask,
            });
        }
    }
}