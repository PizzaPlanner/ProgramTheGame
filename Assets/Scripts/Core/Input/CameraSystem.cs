using Core.Input;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

struct Navigation : IInputActionState
{
    public string[] StateSets => Array.Empty<string>();
    public InputActionMap StateFor(InputActions asset) => asset.Player;
    
    public static InputAction Move => InputActions.For<Navigation>()["Move"];
    public static InputAction Rotate => InputActions.For<Navigation>()["Rotate"];
    public static InputAction Zoom => InputActions.For<Navigation>()["Zoom"];
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(InputActionsSystem))]
partial struct CameraSystem : ISystem
{
    public struct CameraAnchor : IComponentData
    {
        public float3 Position;
        public float CameraDistance;
    }
    
    const float ZOOM_FACTOR = 45f;
    const float ROTATION_FACTOR = 14f;
    const float MOVE_FACTOR = 8f;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurrentGrid>();
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
        
        var cam = Camera.main.transform;
        var camAnchor = SystemAPI.GetSingletonRW<CameraAnchor>();
        
        var shipLtw = SystemAPI.GetComponent<LocalToWorld>(ship);
        
        var wasd = (float2)Navigation.Move.ReadValue<Vector2>();
        var rotate = Navigation.Rotate.ReadValue<Vector2>().x;
        var zoom = Navigation.Zoom.ReadValue<float>();
        
        if(zoom != 0)
            camAnchor.ValueRW.CameraDistance -= zoom * SystemAPI.Time.DeltaTime * ZOOM_FACTOR;
        
        cam.RotateAround(camAnchor.ValueRW.Position, shipLtw.Up, rotate * SystemAPI.Time.DeltaTime * ROTATION_FACTOR);
        cam.position = camAnchor.ValueRW.Position - (float3)cam.forward * camAnchor.ValueRW.CameraDistance;
        new FaceCameraJob
        {
            camForward = cam.forward,
            camLocation = cam.position
        }.ScheduleParallel();
        if(math.all(wasd.xy == 0))
            return;
        var direction = math.normalizesafe(shipLtw.Right * wasd.x + shipLtw.Forward * wasd.y);
        var deltaRot = Quaternion.FromToRotation(shipLtw.Forward, cam.forward);
        
        var euler = math.Euler(deltaRot);
        var camYaw = quaternion.Euler(0, euler.y, 0); // Camera rotation with only yaw
        
        direction = math.mul(camYaw, direction);
        camAnchor.ValueRW.Position += (direction * SystemAPI.Time.DeltaTime * MOVE_FACTOR);
    }
}