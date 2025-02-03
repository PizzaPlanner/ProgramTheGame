using Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
partial struct BlueprintPlacementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<IdentifiablesMappingGroup.Loaded>();
        state.RequireForUpdate<Blueprint>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        // Only run immediately after a tick, so added gases will not spread due to constructions happening a frame late
        if(!state.World.GetExistingSystemManaged<TickBasedSystemGroup>().TickedThisFrame)
            return;
        
        foreach (var (bp, content, e) in SystemAPI.Query<Blueprint, DynamicBuffer<BlueprintContents>>().WithEntityAccess())
        {
            if(!SystemAPI.HasComponent<MassModel>(bp.BuildOn) || !SystemAPI.HasComponent<RoomModel>(bp.BuildOn))
                continue;
            ecb.DestroyEntity(e);
            foreach (var part in content)
            {
                var prefab = part.Prefab.SpawnablePrefab;
                if (SystemAPI.HasComponent<Element>(prefab))
                {
                    ref var massModel = ref SystemAPI.GetComponentRW<MassModel>(bp.BuildOn).ValueRW;
                    var cell = part.Location + bp.Offset;
                    if(!massModel.IsInBounds(cell))
                        massModel.Resize(math.min(massModel.Min, cell), math.max(massModel.Max, cell));
                    ref var cellRW = ref massModel.AbsoluteCellRW(cell);
                    var massData = MassData.From(part.Prefab, part.Matter.Mass, part.Matter.Temperature);
                    cellRW.Add(massData);
                    continue;
                }
                if (!SystemAPI.HasComponent<MassData>(prefab))
                {
                    Debug.LogWarning($"Blueprint {e}: Skipped {part.Prefab}: Unspawnable");
                    continue;
                }
                
                var thingMass = SystemAPI.GetComponent<MassData>(prefab);
                if(part.Matter.Mass > 0)
                    thingMass.Mass = part.Matter.Mass;
                if(part.Matter.Id != default)
                    thingMass.ElementType = part.Matter.Id;
                if(part.Matter.Temperature > 0)
                    thingMass.Temperature = part.Matter.Temperature;
                thingMass = MassData.From(thingMass.ElementType, thingMass.Mass, thingMass.Temperature);
                
                if (SystemAPI.HasComponent<Building>(prefab))
                {
                    var plan = new PlannedConstruction
                    {
                        Prefab = part.Prefab,
                        Location = part.Location,
                        Mode = ConstructionMode.Complete,
                        Parent = bp.BuildOn,
                        Materials = new ConstructionVariant { PrimaryMass = thingMass },
                        Rotation = (byte)(part.Rotation / 90 % 4)
                    };
                    plan.Parent = bp.BuildOn;
                    plan.Location += bp.Offset;
                    plan.Mode = ConstructionMode.Complete;
                    var planEntity = ecb.CreateEntity();
                    ecb.AddComponent(planEntity, plan);
                }
                else if (SystemAPI.HasComponent<OnGrid>(prefab))
                {
                    var thingEntity = ecb.Instantiate(prefab);
                    ecb.SetComponent(thingEntity, LocalTransform.FromPosition(Grids.CGrid.CellCenterToLocal(part.Location + bp.Offset)));
                    ecb.SetComponent(thingEntity, new OnGrid { Grid = bp.BuildOn });
                    ecb.SetComponent(thingEntity, thingMass);
                }
                else
                {
                    Debug.LogWarning($"Blueprint {e}: Skipped {part.Prefab}: Cannot be spawned via Blueprint");
                }
            }
        }
    }
}