using Grids;
using Navigation;
using Units;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space.Systems
{
    partial struct TravelJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        [NativeDisableContainerSafetyRestriction] 
        public BuildUtils.AllLookups lookups;
        [ReadOnly] public BufferLookup<BelongsToShip> shipLookup;

        void Execute(Entity e, ref Task task, ref TaskProgress progress, ref TravelTask travel, ref Sector sector,
                     EnabledRefRO<WorkedOn> workedOn)
        {
            if (task.Target == default)
            {
                task.Origin = default;
                return;
            }
            var shipCells = shipLookup[task.Target].Reinterpret<int2>().AsNativeArray();
            switch (travel.Stage)
            {
                case TravelStage.Validation:
                {
                    if (!lookups.mass.HasComponent(e) 
                        || !lookups.grid.HasComponent(e) 
                        || !VerifyShipIntegrity(task))
                        return;
                    if(!BuildUtils.Transfer(travel.FromLocation, shipCells, 
                           e, 0, ecb, lookups, out var offset))
                        return;
                    BuildUtils.TransferCrew(travel.FromLocation, e, shipCells, offset, lookups);
                    travel.Stage = TravelStage.Preparation;
                    progress.RequiredTicks = 100;
                    progress.PartialTicks = 0;
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
                    if(!BuildUtils.Transfer(e, shipCells, travel.ToLocation, 
                           lookups.grid[travel.ToLocation].BoundsWithBorder.Min - lookups.grid[e].BoundsWithBorder.Scale, ecb, 
                           lookups, out var offset))
                        return;
                    BuildUtils.TransferCrew(e, travel.ToLocation, shipCells, offset, lookups);
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
            var allLookups = new BuildUtils.AllLookups
            {
                building = SystemAPI.GetComponentLookup<Building>(true),
                baked = SystemAPI.GetComponentLookup<BakedIntoRoomMap>(false),
                transform = SystemAPI.GetComponentLookup<LocalTransform>(false),
                grid = SystemAPI.GetComponentLookup<CGrid>(false),
                task = SystemAPI.GetComponentLookup<Task>(false),
                navTree = SystemAPI.GetComponentLookup<NavTree>(false),
                mass = SystemAPI.GetComponentLookup<MassModel>(false),
                onGrid = SystemAPI.GetComponentLookup<OnGrid>(false),
                provider = SystemAPI.GetBufferLookup<TaskProvider>(true),
                crew = SystemAPI.GetBufferLookup<Crew>(true)
            };
            new TravelJob
            {
                ecb = ecb,
                shipLookup = SystemAPI.GetBufferLookup<BelongsToShip>(true),
                lookups = allLookups 
            }.Run();
            
            new LocationTransferJob
            {
                ecb = ecb,
                lookups = allLookups
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