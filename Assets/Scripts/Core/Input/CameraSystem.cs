using Core.Input;
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public struct Navigation : IInputActionState
{
    public string[] StateSets => Array.Empty<string>();
    public InputActionMap StateFor(InputActions asset) => asset.Player;
    
    public static InputAction Move => InputActions.For<Navigation>()["Move"];
    public static InputAction Rotate => InputActions.For<Navigation>()["Rotate"];
    public static InputAction Zoom => InputActions.For<Navigation>()["Zoom"];
}

partial struct CameraSystem : ISystem
{
    const float ROTATION_FACTOR = 14f;
    const float MOVE_FACTOR = 3f;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurrentGrid>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ship = SystemAPI.GetSingleton<CurrentGrid>().Grid;
        if(!SystemAPI.HasComponent<LocalToWorld>(ship))
            return;
        
        var shipLtw = SystemAPI.GetComponent<LocalToWorld>(ship);
        var cam = Camera.main.transform;
        
        var wasd = (float2)Navigation.Move.ReadValue<Vector2>();
        var rotate = Navigation.Rotate.ReadValue<Vector2>().x;
        var zoom = Navigation.Zoom.ReadValue<float>();
        
        if(zoom != 0)
            cam.position += cam.forward * zoom * SystemAPI.Time.DeltaTime * ROTATION_FACTOR;
        
        cam.RotateAround(cam.position, shipLtw.Up, rotate * SystemAPI.Time.DeltaTime * ROTATION_FACTOR);
        
        if(math.all(wasd.xy == 0))
            return;
        var direction = math.normalizesafe(shipLtw.Right * wasd.x + shipLtw.Forward * wasd.y);
        var deltaRot = Quaternion.FromToRotation(shipLtw.Forward, cam.forward);
        
        var euler = math.Euler(deltaRot);
        var camYaw = quaternion.Euler(0, euler.y, 0); // Camera rotation with only yaw
        
        direction = math.mul(camYaw, direction);
        cam.position += (Vector3)(direction * SystemAPI.Time.DeltaTime * MOVE_FACTOR);
    }
}