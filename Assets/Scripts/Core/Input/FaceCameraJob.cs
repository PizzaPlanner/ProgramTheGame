using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct FaceCameraDirection : IComponentData { }

[WithAll(typeof(FaceCameraDirection))]
partial struct FaceCameraJob : IJobEntity
{
    public float3 camForward, camLocation;

    void Execute(ref LocalTransform transform, in LocalToWorld ltw)
    {
        var totalRot = quaternion.LookRotation(camForward, math.up());
        transform.Rotation = totalRot;
    }
}