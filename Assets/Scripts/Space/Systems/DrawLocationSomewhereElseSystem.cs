using Units;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Space.Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(CurrentGridSystem))]
    partial struct DrawLocationSomewhereElseSystem : ISystem
    {
        const int TRAVEL_Y = 15;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CurrentGrid>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currentGrid = SystemAPI.GetSingleton<CurrentGrid>();
            var camChanged = currentGrid.Grid != currentGrid.LastGrid;
            if(!SystemAPI.HasComponent<Sector>(currentGrid.Grid))
                return;
            
            var camLocation = SystemAPI.GetComponent<Sector>(currentGrid.Grid).PositionInSpace;
            if(camChanged)
                foreach (var (loc, transform) in SystemAPI.Query<Sector, RefRW<LocalTransform>>()
                                                          .WithNone<TravelTask>())
                {
                    var delta = loc.PositionInSpace - camLocation;
                    var pos = new float3(delta.x, 0, delta.y);
                    transform.ValueRW.Position = pos * Sector.LENGTH;
                }
            else
                foreach (var (loc, transform) in SystemAPI.Query<Sector, RefRW<LocalTransform>>()
                                                          .WithChangeFilter<Sector>().WithNone<TravelTask>())
                {
                    var delta = loc.PositionInSpace - camLocation;
                    var pos = new float3(delta.x, 0, delta.y);
                    transform.ValueRW.Position = pos * Sector.LENGTH;
                }
            
            foreach (var (travel, task, progress, transform, e) in SystemAPI.Query<TravelTask, Task, TaskProgress, RefRW<LocalTransform>>().WithEntityAccess())
            {
                var loc = SystemAPI.GetComponent<Sector>(e).PositionInSpace;
                var fromLoc = SystemAPI.GetComponent<Sector>(travel.FromLocation).PositionInSpace;
                var toLoc = SystemAPI.GetComponent<Sector>(travel.ToLocation).PositionInSpace;
                var deltaToCam = loc - camLocation;
                var travelDelta = toLoc - fromLoc;
                var offsetByCam = new float3(deltaToCam.x, 0, deltaToCam.y) * Sector.LENGTH;
                transform.ValueRW.Position = offsetByCam;
                if (math.all(loc == fromLoc))
                    transform.ValueRW.Position += travel.TakeoffPointInFromSector;
                
                switch (travel.Stage)
                {
                    case TravelStage.Takeoff when progress.Progress < 0.5f:
                    {
                        var from = float3.zero;
                        var to = math.up() * TRAVEL_Y;
                        transform.ValueRW.Position += math.lerp(from, to, progress.Progress * 2);
                        break;
                    }
                    case TravelStage.Takeoff:
                    {
                        transform.ValueRW.Position += math.up() * TRAVEL_Y;
                        var deltaRot = quaternion.LookRotationSafe(new float3(travelDelta.x, 0, travelDelta.y), math.up());
                        transform.ValueRW.Rotation = Quaternion.Lerp(quaternion.identity, deltaRot, progress.Progress * 2 - 1);
                        break;
                    }
                    case TravelStage.Arrival when progress.Progress > 0.5f:
                    {
                        var to2 = float3.zero;
                        var from2 = math.up() * TRAVEL_Y;
                        transform.ValueRW.Position += math.lerp(from2, to2, progress.Progress * 2 - 1);
                        break;
                    }
                    case TravelStage.Arrival:
                    {
                        var deltaRot = quaternion.LookRotationSafe(new float3(travelDelta.x, 0, travelDelta.y), math.up());
                        transform.ValueRW.Rotation = Quaternion.Lerp(deltaRot, Quaternion.identity, progress.Progress * 2);
                        break;
                    }
                    case TravelStage.InFlight:
                        var from3 = new float3(fromLoc.x, 0, fromLoc.y) * Sector.LENGTH;
                        var to3 = new float3(toLoc.x, 0, toLoc.y) * Sector.LENGTH;
                        transform.ValueRW.Position += math.lerp(from3, to3, progress.Progress) + math.up() * TRAVEL_Y;
                        break;
                }
            }
        }
    }
}