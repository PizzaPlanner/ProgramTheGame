using Identifiables;
using Units;
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
    public Entity constructTaskPrefab, orderTaskPrefab;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<RoomModel> roomLookup;
    [ReadOnly] public ComponentLookup<GridBounds> boundsLookup;
    [ReadOnly] public ComponentLookup<Building> buildingLookup;
    public ComponentLookup<GridStructure> structureLookup;

    void Execute(Entity e, in PlannedConstruction planned)
    {
        ecb.DestroyEntity(e);
        
        // Get prefab from id
        var prefab = planned.Prefab.SpawnablePrefab;
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
        
        var building = buildingLookup[prefab];

        // Create Part
        var entity = ecb.Instantiate(prefab);
        ecb.SetComponent(entity, planned.ToTransform);
        ecb.AddComponent(entity, new Parent { Value = planned.Parent });
        ecb.SetComponent(entity, area);
        
        if(building.Layer != BuildingLayer.Floor)
            Rooms.AddOccupiedArea(area);
        if (planned.IsGhost)
        {
            CreateBuildTasks(planned, entity, building);
            ecb.AddComponent(entity,
                new FixGhostVisualsAndPhysicsSystem.FixGhostandChildren { GhostBuildable = true });
        }
        else
        {
            ecb.AddComponent<PendingGridUpdate>(planned.Parent);
            ecb.RemoveComponent<BuildMaterials>(entity);
            if(building.Layer == BuildingLayer.Wall)
                Rooms.AddWall(area);
            area = area.Extend(2);
            var gridBbox = structureLookup[planned.Parent];
            gridBbox.BoundsMin = math.min(gridBbox.BoundsMin, area.Min);
            gridBbox.BoundsMax = math.max(gridBbox.BoundsMax, area.Max);
            structureLookup[planned.Parent] = gridBbox;
        }
    }

    void CreateBuildTasks(PlannedConstruction planned, Entity entity, Building building)
    {
        var constructTask = ecb.Instantiate(constructTaskPrefab);
        ecb.SetComponent(constructTask, new Task
        {
            Target = entity, 
            Priority = TaskPriority.MediumPriority,
            Origin = entity
        });
        ecb.AddComponent(entity, new UnderConstruction
        {
            Duration = building.Duration
        });
        foreach (var requirement in planned.Materials)
        {
            var order = ecb.Instantiate(orderTaskPrefab);
            ecb.SetComponent(order, new Task
            {
                Priority = TaskPriority.MediumPriority,
                Origin = entity
            });
            ecb.SetComponent(order, new ItemOrderTask
            {
                Amount = requirement.Amount, 
                Item = requirement.Item
            });
            ecb.AppendToBuffer(constructTask, new TaskDependencies
            {
                Dependency = order
            });
        }
    }
}