using Grids;
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
        public ComponentLookup<CGrid> gridLookup;
        public ComponentLookup<NavTree> navTreeLookup;
        public ComponentLookup<MassModel> massLookup;
        public ComponentLookup<RoomModel> roomLookup;
        [ReadOnly] public ComponentLookup<Building> buildingLookup;
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
                        || !gridLookup.HasComponent(e) 
                        || !VerifyShipIntegrity(task))
                    {
                        task.IsCancelable = true;
                        return;
                    }
                    var baked = bakedLookup[task.Origin];
                    var fromBounds = baked.Area.Extend(5);
                    if(!BuildUtils.Transfer(travel.FromLocation, fromBounds, e, fromBounds.Min - baked.Area.Min, ecb, 
                        navTreeLookup, massLookup, roomLookup, bakedLookup, gridLookup, buildingLookup))
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
                case TravelStage.PreArrival:
                {
                    if(travel.LandingArea.Size == 0)
                        return;
                    progress.RequiredTicks = 100;
                    progress.PartialTicks = 0;
                    travel.Stage = TravelStage.PreArrival;
                    break;
                }
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
                    if(!BuildUtils.Transfer(e, gridLookup[e].BoundsWithBorder, travel.ToLocation, 
                           gridLookup[travel.ToLocation].BoundsWithBorder.Min - gridLookup[e].BoundsWithBorder.Scale, ecb, 
                           navTreeLookup, massLookup, roomLookup, bakedLookup, gridLookup, buildingLookup))
                        return;
                    ecb.DestroyEntity(e);
                }break;
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
                gridLookup = SystemAPI.GetComponentLookup<CGrid>(false),
                massLookup = SystemAPI.GetComponentLookup<MassModel>(false),
                roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
                navTreeLookup = SystemAPI.GetComponentLookup<NavTree>(false),
                buildingLookup = SystemAPI.GetComponentLookup<Building>(true)
            }.Run();
            
            new LocationTransferJob
            {
                ecb = ecb,
                massLookup = SystemAPI.GetComponentLookup<MassModel>(false),
                roomLookup = SystemAPI.GetComponentLookup<RoomModel>(false),
                navTreeLookup = SystemAPI.GetComponentLookup<NavTree>(false),
                gridLookup = SystemAPI.GetComponentLookup<CGrid>(false)
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