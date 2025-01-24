using Identifiables;
using Navigation;
using Units;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

public partial struct BuildJob : IJobEntity
{
    static readonly Identifiable ConstructTaskPrefab = Identifiable.FromName("BuildTask");
    static readonly Identifiable OrderTaskPrefab = Identifiable.FromName("ItemOrderTask");
    
    public EntityCommandBuffer ecb;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<MassModel> massLookup;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<NavLayers> navLayerLookup;
    [ReadOnly] public ComponentLookup<GridBounds> boundsLookup;
    [ReadOnly] public ComponentLookup<Building> buildingLookup;
    [ReadOnly] public ComponentLookup<Ghost> ghostLookup;
    public ComponentLookup<GridStructure> structureLookup;
    
    void Execute(Entity e, in PlannedConstruction planned)
    {
        ecb.DestroyEntity(e);
        
        // Get prefab from id
        var prefab = planned.Prefab.SpawnablePrefab;
        if (prefab == Entity.Null || !boundsLookup.TryGetComponent(prefab, out var bounds))
        {
            if(structureLookup.TryGetComponent(prefab, out var s))
                bounds = s.LocalAABB;
            else
            {
                Debug.LogWarning($"Discarded invalid pending part with ID {planned.Prefab.DebugId}");
                return;
            }
        }

        if (!navLayerLookup.TryGetComponent(planned.Parent, out var navigation))
        {
            Debug.LogError($"Can not build on {planned.Parent}");
            return;
        }

        var area = bounds.WithOffset(planned.Location);
        
        if (!buildingLookup.TryGetComponent(prefab, out var building))
        {
            Debug.Log($"Discarded pending part that is not a building");
            return;
        }
        
        if (navigation.IsOccupiedForPlacement(area, building.Layer) && planned.Mode != ConstructionMode.PlacementPreview)
        {
            Debug.LogWarning($"Discarded pending part in invalid or occupied area {area}");
            return;
        }
        
        var isPlacing = planned.Mode == ConstructionMode.PlacementPreview || ghostLookup.HasComponent(planned.Parent);
        var isUnderConstruction = planned.Mode == ConstructionMode.UnderConstruction;

        // Create Part
        var entity = ecb.Instantiate(prefab);
        ecb.SetComponent(entity, planned.ToTransform);
        ecb.SetComponent(entity, area);
        ecb.AddComponent(entity, new Parent { Value = planned.Parent });
        
        if (isPlacing)
        {
            ecb.AddComponent<CurrentGhost>(entity);
            ecb.AddComponent(entity, new Ghost { GhostBuildable = !navigation.IsOccupiedForPlacement(area, building.Layer) });
            ecb.SetComponent(entity, planned.Materials);
        }
        else if (isUnderConstruction)
        {
            navigation.AddOccupiedArea(area, building.Layer.AsGhostLayer(), Entity.Null);   
            CreateBuildTasks(planned, entity, building);
            ecb.AddComponent(entity, new Ghost { GhostBuildable = true });
            ecb.SetComponent(entity, new OnGrid { Grid = planned.Parent });
            ecb.SetComponent(entity, planned.Materials);
            ecb.AddBuffer<Inventory>(entity);
            ecb.AddComponent(entity,  new ToBeBakedIntoRoomMap { Grid = planned.Parent, Area = area, Layer = building.Layer.AsGhostLayer() });
            for (var i = 0; i < planned.Materials.BuildMaterials.Length; i++)
            {
                var mat = planned.Materials.BuildMaterials[i];
                ecb.AppendToBuffer(entity, new Inventory
                {
                    Type = ItemId.From(mat.Item),
                    Transfer = Inventory.TransferDirection.In,
                    IsTypeLocked = true
                });
            }
        }
        else
        {
            navigation.AddOccupiedArea(area, building.Layer, Entity.Null);
            ecb.RemoveComponent<ConstructionVariant>(entity);
            if(planned.Materials.PrimaryMass.ElementType != default)
                ecb.SetComponent(entity, planned.Materials.PrimaryMass);
            ecb.SetComponent(entity, new OnGrid { Grid = planned.Parent });
            
            ref var gridBbox = ref structureLookup.GetRefRW(planned.Parent).ValueRW;
            gridBbox.BoundsMin = math.min(gridBbox.BoundsMin, area.Min - 4);
            gridBbox.BoundsMax = math.max(gridBbox.BoundsMax, area.Max + 4);
            ref var massModel = ref massLookup.GetRefRW(planned.Parent).ValueRW;
            massModel.Resize(gridBbox.BoundsMin, gridBbox.BoundsMax);
            
            ecb.AddComponent(entity, new ToBeBakedIntoRoomMap { Grid = planned.Parent, Area = area, Layer = building.Layer });
        }
    }

    void CreateBuildTasks(PlannedConstruction planned, Entity entity, Building building)
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
            RequiredValue = building.Duration
        });
        ecb.AddComponent(entity, new UnderConstruction { });
        foreach (var requirement in planned.Materials.BuildMaterials)
        {
            if(requirement.Item == default)
                continue;
            var order = Task.Schedule(OrderTaskPrefab, ecb, new Task
            {
                Priority = TaskPriority.MediumPriority,
                Origin = constructTask,
                CrewProvider = planned.Parent
            });
            ecb.SetComponent(order, new ItemOrderTask
            {
                Amount = (ushort)requirement.Amount, 
                Item = ItemId.From(requirement.Item),
                ParentGrid = planned.Parent,
                Requester = entity,
            });
        }
    }
}