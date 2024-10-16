using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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
        foreach (var (bp, content, e) in SystemAPI.Query<Blueprint, DynamicBuffer<BlueprintContents>>().WithEntityAccess())
        {
            foreach (var part in content)
            {
                var prefab = part.Prefab.SpawnablePrefab;
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
                        Materials = new ConstructionVariant { PrimaryMass = thingMass }
                    };
                    plan.Parent = bp.BuildOn;
                    plan.Location += bp.Offset;
                    plan.Mode = ConstructionMode.Complete;
                    var planEntity = ecb.CreateEntity();
                    ecb.AddComponent(planEntity, plan);
                }
                else if (SystemAPI.HasComponent<Unit>(prefab))
                {
                    var unitEntity = ecb.Instantiate(part.Prefab.SpawnablePrefab);
                    ecb.SetComponent(unitEntity, LocalTransform.FromPosition(GridStructure.CellToLocal(part.Location + bp.Offset)));
                    ecb.SetComponent(unitEntity, new OnGrid { Grid = bp.BuildOn });
                    ecb.SetComponent(unitEntity, thingMass);
                }
                else
                {
                    Debug.LogWarning($"Blueprint {e}: Skipped {part.Prefab}: Cannot be spawned via Blueprint");
                }
            }
            ecb.DestroyEntity(e);
        }
    }
}