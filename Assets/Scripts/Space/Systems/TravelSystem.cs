using Navigation;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space.Systems
{
    partial struct TravelJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        [ReadOnly] public ComponentLookup<BakedIntoRoomMap> bakedLookup;
        [ReadOnly] public ComponentLookup<NavLayers> navLayersLookup;
        [ReadOnly] public ComponentLookup<GridStructure> gridLookup;
        void Execute(Entity e, ref Task task, ref TaskProgress progress, ref TravelTask travel, EnabledRefRO<WorkedOn> workedOn)
        {
            if (task.Target == default)
            {
                task.Origin = default;
                return;
            }

            switch (travel.Stage)
            {
                case TravelStage.Validation:
                {
                    if (!VerifyShipIntegrity(task))
                    {
                        task.IsCancelable = true;
                        return;
                    }
                    travel.Stage = TravelStage.Preparation;
                    progress.RequiredTicks = 100;
                    progress.PartialTicks = 0;
                    // Don't modify travel after this call!
                    DisconnectShip(e, task, travel);
                    task.IsCancelable = false;
                    return;
                };break;
                case TravelStage.Preparation:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    travel.Stage = TravelStage.Takeoff;
                    progress.RequiredTicks = 500;
                    progress.PartialTicks = 0;
                    ecb.AddComponent(travel.RouteLocation, new LocationTransferAnimation { Duration = 1 });
                    Debug.Log($"Taking off...");
                }break;
                case TravelStage.Takeoff:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    travel.Stage = TravelStage.InFlight;
                    progress.RequiredTicks = (uint)(travel.Distance * 100);
                    progress.PartialTicks = 0;
                    task.Executor = task.Origin;
                    Debug.Log($"In Hyperspace");
                }break;
                case TravelStage.InFlight:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    progress.RequiredTicks = 100;
                    progress.PartialTicks = 0;
                    travel.Stage = TravelStage.Arrival;
                    Debug.Log($"Arrived at Destination. Landing...");
                    ecb.AddComponent(travel.RouteLocation, new LocationTransferAnimation { Duration = 1, IsLanding = true});
                }break;
                case TravelStage.Arrival:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    ReconnectShip(e, travel);
                    travel.Stage = TravelStage.Completed;
                }break;
                case TravelStage.Completed:
                {
                    ecb.DestroyEntity(e);
                    ecb.DestroyEntity(travel.RouteLocation);
                }break;
            }
        }

        void ReconnectShip(Entity e, TravelTask travel)
        {
            var grid = gridLookup[travel.RouteLocation];
            var navi = navLayersLookup[travel.RouteLocation];
            var shipParts = navi.GetAllEntitiesIn(grid.LocalAABB);
            Debug.Log($"Reconnecting {shipParts.Count} parts");
            foreach (var part in shipParts)
            {
                if (!bakedLookup.TryGetComponent(part, out var partBaked))
                {
                    Debug.LogWarning($"Something off about {part}");
                    continue;
                }
                ecb.AddComponent(part, new TransferToLocation
                {
                    DeltaOffset = -new int2(grid.BoundsMax.x, 0),
                    TargetLocation = travel.ToLocation
                });
            }
        }

        void DisconnectShip(Entity e, Task task, TravelTask travel)
        {
            travel.RouteLocation = ecb.Instantiate(Core.ID.Hyperspace.SpawnablePrefab);
            ecb.SetComponent(e, travel); // Reassign to ensure remapping
            
            var baked = bakedLookup[task.Origin];
            var navi = navLayersLookup[travel.FromLocation];
            var fromGrid = gridLookup[travel.FromLocation];
            var shipParts = navi.GetAllEntitiesIn(baked.Area.Extend(5));
            Debug.Log($"Disconnecting {shipParts.Count} parts");
            foreach (var part in shipParts)
            {
                if (!bakedLookup.TryGetComponent(part, out var partBaked))
                {
                    Debug.LogWarning($"Something off about {part}");
                    continue;
                }
                ecb.AddComponent(part, new TransferToLocation
                {
                    DeltaOffset = -baked.Area.Min,
                    TargetLocation = travel.RouteLocation
                });
            }
            ecb.SetComponent(travel.RouteLocation, LocalTransform.FromPosition(new float3(baked.Area.Min.x, 0, baked.Area.Min.y)));
        }

        bool VerifyShipIntegrity(Task task)
        {
            return true;
        }
    }
    
    partial struct TravelSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);
            new TravelJob
            {
                ecb = ecb,
                bakedLookup = SystemAPI.GetComponentLookup<BakedIntoRoomMap>(true),
                navLayersLookup = SystemAPI.GetComponentLookup<NavLayers>(true),
                gridLookup = SystemAPI.GetComponentLookup<GridStructure>(true),
            }.Run();
            
            new LocationTransferJob
            {
                ecb = ecb,
                massLookup = SystemAPI.GetComponentLookup<MassModel>(false),
                navLayersLookup = SystemAPI.GetComponentLookup<NavLayers>(false),
                roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
                navTreeLookup = SystemAPI.GetComponentLookup<NavTree>(false)
            }.Run();

            foreach (var (anim, transform, e) in
                     SystemAPI.Query<RefRW<LocationTransferAnimation>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                anim.ValueRW.Progress += SystemAPI.Time.DeltaTime;
                transform.ValueRW.Position.y = anim.ValueRW.Offset;
                if (anim.ValueRW.Progress > anim.ValueRW.Duration)
                {
                    ecb.RemoveComponent<LocationTransferAnimation>(e);
                    transform.ValueRW.Position.y = anim.ValueRW.Offset;
                }
            }
        }
    }
}