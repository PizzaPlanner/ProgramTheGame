using System;
using Unity.Entities;
using UnityEngine;

public class StatusProviderAuthoring : MonoBehaviour
{
    [Serializable]
    public class StatusProviderData
    {
        public StatusAuthoring Status;
        public ushort MinValue;
        public ushort MaxValue;
    }
    
    public StatusProviderData[] Values;
    
    class Baker : Baker<StatusProviderAuthoring>
    {
        public override void Bake(StatusProviderAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent<StatusProviderInput>(e);
            var statuses = AddBuffer<StatusProvider>(e);
            foreach(var status in authoring.Values)
                statuses.Add(new StatusProvider
                {
                    MaxValue = status.MaxValue,
                    MinValue = status.MinValue,
                    Status = status.Status.Id
                });
        }
    }
}