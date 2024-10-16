using Identifiables;
using Units;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

public partial struct BuildJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Entity constructTaskPrefab, orderTaskPrefab;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<RoomModel> roomLookup;
    [NativeDisableContainerSafetyRestriction] public ComponentLookup<MassModel> massLookup;
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
                Debug.LogWarning($"Discarded invalid pending part with ID {planned.Prefab.StableId}");
                return;
            }
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
        
        if (!buildingLookup.TryGetComponent(prefab, out var building))
        {
            Debug.Log($"Discarded pending part that is not a building");
            return;
        }
        
        if (Rooms.IsOccupied(area, building.Layer) && planned.Mode != ConstructionMode.PlacementPreview)
        {
            Debug.LogWarning($"Discarded pending part in invalid or occupied area {bounds.Min}/{bounds.Max}");
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
            ecb.AddComponent(entity, new Ghost { GhostBuildable = !Rooms.IsOccupied(area, building.Layer) });
            ecb.SetComponent(entity, planned.Materials);
            ecb.RemoveComponent<MassData>(entity);
        }
        else if (isUnderConstruction)
        {
            CreateBuildTasks(planned, entity, building);
            ecb.AddComponent(entity, new Ghost { GhostBuildable = true });
            ecb.SetComponent(entity, new OnGrid { Grid = planned.Parent });
            ecb.SetComponent(entity, planned.Materials);
            ecb.RemoveComponent<MassData>(entity);
            ecb.AddComponent(entity, new BakedIntoRoomMap { Grid = planned.Parent, Area = area, Layer = building.Layer });
            Rooms.ReserveOccupiedArea(area, building.Layer, entity, ecb); 
        }
        else
        {
            ecb.RemoveComponent<ConstructionVariant>(entity);
            ecb.AddComponent<PendingGridUpdate>(planned.Parent);
            if(planned.Materials.PrimaryMass.ElementType != default)
                ecb.SetComponent(entity, planned.Materials.PrimaryMass);
            ecb.SetComponent(entity, new OnGrid { Grid = planned.Parent });
            
            var gridBbox = structureLookup[planned.Parent];
            gridBbox.BoundsMin = math.min(gridBbox.BoundsMin, area.Min - 4);
            gridBbox.BoundsMax = math.max(gridBbox.BoundsMax, area.Max + 4);
            structureLookup[planned.Parent] = gridBbox;
            var massModel = massLookup[planned.Parent];
            massModel.Resize(gridBbox.BoundsMin, gridBbox.BoundsMax);
            massLookup[planned.Parent] = massModel;
            
            ecb.AddComponent(entity, new BakedIntoRoomMap { Grid = planned.Parent, Area = area, Layer = building.Layer });
            Rooms.ReserveOccupiedArea(area, building.Layer, entity, ecb);
            switch (building.Layer)
            {
                case BuildingLayer.Wall:
                {
                    massModel.AddWall(area);
                    break;
                }
                case BuildingLayer.FloorLevel:
                {
                    massModel.AddFloor(area);
                    break;
                }
                default:
                    break;
            }
        }
    }

    void CreateBuildTasks(PlannedConstruction planned, Entity entity, Building building)
    {
        var constructTask = ecb.Instantiate(constructTaskPrefab);
        ecb.SetComponent(constructTask, new Task
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
        ecb.AddBuffer<TaskProvider>(entity);
        ecb.AppendToBuffer(entity, new TaskProvider { Task = constructTask });
        ecb.AddComponent(entity, new UnderConstruction { });
        foreach (var requirement in planned.Materials.BuildMaterials)
        {
            var order = ecb.Instantiate(orderTaskPrefab);
            ecb.SetComponent(order, new Task
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
            ecb.AppendToBuffer(constructTask, new TaskProvider()
            {
                Task = order
            });
        }
    }
    
    static int mod(int x, int m) {
        return (x%m + m)%m;
    }
}