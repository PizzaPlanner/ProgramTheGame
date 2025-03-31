using Core.Input;
using Grids;
using InputStates;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(InputActionsSystem))]
partial struct CameraSystem : ISystem
{
    public struct CameraAnchor : IComponentData
    {
        public float2 Offset;
        public float Rotation;
        public float CameraDistance { 
            get => _distance; 
            set => _distance = math.clamp(value, MinDistance, MaxDistance); 
        }
        float _distance;
        
        public float ZoomFactor => 2f * CameraDistance;
        public float MoveFactor => 0.5f * CameraDistance + 5;
        public float MinDistance => 5f;
        public float MaxDistance => 500f;
        public float RotationFactor => 3f;
    }
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurrentGrid>();
        state.RequireForUpdate<Player>();
        state.EntityManager.CreateSingleton(new CameraAnchor
        {
            CameraDistance = 20
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        var ship = SystemAPI.GetSingleton<CurrentGrid>().Grid;
        if(!SystemAPI.HasComponent<LocalToWorld>(ship))
            return;
        if (!SystemAPI.HasComponent<CGrid>(ship))
        {
            if(SystemAPI.HasComponent<Unvisited>(ship))
                state.EntityManager.RemoveComponent<Unvisited>(ship);
            return;
        }
        if(Camera.main == null)
            return;
        
        var cam = Camera.main.transform;
        var camAnchor = SystemAPI.GetSingletonRW<CameraAnchor>();
        
        var shipLtw = SystemAPI.GetComponent<LocalToWorld>(ship);
        var shipBounds = SystemAPI.GetComponent<CGrid>(ship).BoundsWithBorder;
        
        var wasd = (float2)Player.Move.ReadValue<Vector2>();
        var rotate = Player.Rotate.ReadValue<Vector2>().x;
        var zoom = InputActionsSystem.IsPointerOverUI ? 0 : Player.Zoom.ReadValue<float>();
        
        if(zoom != 0)
            camAnchor.ValueRW.CameraDistance -= zoom * SystemAPI.Time.DeltaTime * camAnchor.ValueRW.ZoomFactor;
        
        camAnchor.ValueRW.Offset = math.clamp(camAnchor.ValueRW.Offset, shipBounds.Min, shipBounds.Max + 1);
        camAnchor.ValueRW.Rotation += rotate * SystemAPI.Time.DeltaTime * camAnchor.ValueRW.RotationFactor;
        var offset = camAnchor.ValueRW.Offset.xyy;
        offset.y = 0;
        var lookAtPoint = shipLtw.Position + math.mul(shipLtw.Rotation, offset);
        var userRotation = quaternion.AxisAngle(math.up(), camAnchor.ValueRW.Rotation);
        var camPitch = quaternion.AxisAngle(math.right(), math.radians(55));
        cam.rotation = math.mul(math.mul(userRotation, shipLtw.Rotation), camPitch);
        cam.position = lookAtPoint - (float3)cam.forward * camAnchor.ValueRW.CameraDistance;
        new FaceCameraJob
        {
            camForward = cam.forward,
            camLocation = cam.position
        }.ScheduleParallel();
        if(math.all(wasd.xy == 0))
            return;
        var direction = math.normalizesafe(math.right() * wasd.x + math.forward() * wasd.y);
        // var deltaRot = Quaternion.FromToRotation(math.forward(), cam.forward);
        
        var euler = math.Euler(cam.rotation);
        var camYaw = quaternion.Euler(0, euler.y, 0); // Camera rotation with only yaw
        
        direction = math.mul(userRotation, direction);
        camAnchor.ValueRW.Offset += (direction * SystemAPI.Time.DeltaTime * camAnchor.ValueRW.MoveFactor).xz;
    }
}