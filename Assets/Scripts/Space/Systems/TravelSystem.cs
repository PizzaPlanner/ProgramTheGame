using Core;
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
        public ComponentLookup<BakedIntoRoomMap> bakedLookup;
        public ComponentLookup<NavLayers> navLayersLookup;
        public ComponentLookup<GridStructure> gridLookup;
        public ComponentLookup<NavTree> navTreeLookup;
        public ComponentLookup<MassModel> massLookup;
        public ComponentLookup<RoomModel> roomLookup;
        void Execute(Entity e, ref Task task, ref TaskProgress progress, ref TravelTask travel, ref Sector sector, EnabledRefRO<WorkedOn> workedOn)
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
                    if (!massLookup.HasComponent(e) 
                        || !navLayersLookup.HasComponent(e) 
                        || !VerifyShipIntegrity(task))
                    {
                        task.IsCancelable = true;
                        return;
                    }
                    var baked = bakedLookup[task.Origin];
                    var fromBounds = baked.Area.Extend(5);
                    if(!BuildUtils.Transfer(travel.FromLocation, fromBounds, e, fromBounds.Min - baked.Area.Min, ecb, 
                        navLayersLookup, navTreeLookup, massLookup, roomLookup, bakedLookup, gridLookup))
                        return;
                    
                    travel.Stage = TravelStage.Preparation;
                    progress.RequiredTicks = 100;
                    progress.PartialTicks = 0;
                    task.IsCancelable = false;
                }break;
                case TravelStage.Preparation:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    travel.Stage = TravelStage.Takeoff;
                    progress.RequiredTicks = 500;
                    progress.PartialTicks = 0;
                }break;
                case TravelStage.Takeoff:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    travel.Stage = TravelStage.InFlight;
                    progress.RequiredTicks = (uint)travel.Distance / 10;
                    progress.PartialTicks = 0;
                    task.Executor = task.Origin;
                }break;
                case TravelStage.InFlight:
                {
                    progress.PartialTicks += 1;
                    sector.PositionInSpace = math.lerp(travel.From.PositionInSpace, travel.To.PositionInSpace, progress.Progress);
                    if(progress.Progress < 1)
                        return;
                    progress.RequiredTicks = 100;
                    progress.PartialTicks = 0;
                    travel.Stage = TravelStage.Arrival;
                    sector.PositionInSpace = travel.To.PositionInSpace;
                }break;
                case TravelStage.Arrival:
                {
                    progress.PartialTicks += 1;
                    if(progress.Progress < 1)
                        return;
                    progress.RequiredTicks = 0;
                    progress.PartialTicks = 0;
                    travel.Stage = TravelStage.Landing;
                }break;
                case TravelStage.Landing:
                {
                    if(!BuildUtils.Transfer(e, gridLookup[e].LocalAABB, travel.ToLocation, 
                           gridLookup[travel.ToLocation].BoundsMin - gridLookup[e].LocalAABB.Scale, ecb, 
                           navLayersLookup, navTreeLookup, massLookup, roomLookup, bakedLookup, gridLookup))
                        return;
                    ecb.DestroyEntity(e);
                }break;
            }
        }

        void ReconnectShip(Entity e, TravelTask travel)
        {
            var grid = gridLookup[e];
            var navi = navLayersLookup[e];
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
            var baked = bakedLookup[task.Origin];
            var fromGrid = gridLookup[travel.FromLocation];
            var navi = navLayersLookup[travel.FromLocation];
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
                    TargetLocation = e
                });
            }
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
            state.CompleteDependency();
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUnmanaged);
            new TravelJob
            {
                ecb = ecb,
                bakedLookup = SystemAPI.GetComponentLookup<BakedIntoRoomMap>(false),
                navLayersLookup = SystemAPI.GetComponentLookup<NavLayers>(false),
                gridLookup = SystemAPI.GetComponentLookup<GridStructure>(false),
                massLookup = SystemAPI.GetComponentLookup<MassModel>(false),
                roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
                navTreeLookup = SystemAPI.GetComponentLookup<NavTree>(false)
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