using Unity.Entities;

public struct LogicChip : IComponentData
{
    public UnityObjectRef<ChipScript> ManagedScript;    
}