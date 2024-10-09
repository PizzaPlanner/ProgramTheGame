using Identifiables;
using Units;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

public enum ConstructionMode
{
    PlacementPreview,
    UnderConstruction,
    Complete
}

public struct ConstructionVariant : IComponentData
{
    public struct Material
    {
        public Identifiable Item;
        public Mass Amount;
        public bool IsVariant;
    }
    
    public FixedList64Bytes<Material> BuildMaterials;
    public MassData PrimaryMass => BuildMaterials.Length > 0 
        ? MassData.From(BuildMaterials[0].Item, BuildMaterials[0].Amount, 0) 
        : MassData.From(Identifiable.FromName("Voidite"), 1000, 0); 
}

public struct PlannedConstruction : IComponentData
{
    public Entity Parent;
    public Identifiable Prefab;
    public Cell Location;
    public byte Rotation;
    public ConstructionMode Mode;
    public ConstructionVariant Materials;
    public LocalTransform ToTransform => LocalTransform.FromPosition(GridStructure.CellToLocal(Location));
}

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
        
        if (Rooms.IsOccupied(area) && planned.Mode != ConstructionMode.PlacementPreview)
        {
            Debug.LogWarning($"Discarded pending part in invalid or occupied area {bounds.Min}/{bounds.Max}");
            return;
        }
        
        if (!buildingLookup.TryGetComponent(prefab, out var building))
        {
            Debug.Log($"Discarded pending part that is not a building");
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
            ecb.AddComponent(entity, new Ghost { GhostBuildable = !Rooms.IsOccupied(area) });
            ecb.SetComponent(entity, planned.Materials);
        }
        else if (isUnderConstruction)
        {
            CreateBuildTasks(planned, entity, building);
            ecb.AddComponent(entity, new Ghost { GhostBuildable = true });
            ecb.AddComponent(entity, new BakedIntoRoomMap { Grid = planned.Parent, Area = area });
            ecb.SetComponent(entity, new OnGrid { Grid = planned.Parent });
            ecb.SetComponent(entity, planned.Materials);
            if(building.Layer != BuildingLayer.Floor)
                Rooms.AddOccupiedArea(area);
        }
        else
        {
            ecb.RemoveComponent<ConstructionVariant>(entity);
            ecb.AddComponent<PendingGridUpdate>(planned.Parent);
            ecb.AddComponent(entity, planned.Materials.PrimaryMass);
            ecb.AddComponent(entity, new BakedIntoRoomMap { Grid = planned.Parent, Area = area });
            ecb.SetComponent(entity, new OnGrid { Grid = planned.Parent });
            
            var gridBbox = structureLookup[planned.Parent];
            gridBbox.BoundsMin = math.min(gridBbox.BoundsMin, area.Min - 2);
            gridBbox.BoundsMax = math.max(gridBbox.BoundsMax, area.Max + 2);
            structureLookup[planned.Parent] = gridBbox;
            var massModel = massLookup[planned.Parent];
            massModel.Resize(gridBbox.BoundsMin, gridBbox.BoundsMax);
            massLookup[planned.Parent] = massModel;
            
            switch (building.Layer)
            {
                case BuildingLayer.Wall or BuildingLayer.Door:
                {
                    massModel.AddWall(area);
                    Rooms.AddWall(area);
                    Rooms.AddOccupiedArea(area);
                    break;
                }
                case BuildingLayer.Floor:
                {
                    massModel.AddFloor(area);
                    break;
                }
                default:
                    Rooms.AddOccupiedArea(area);
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
}