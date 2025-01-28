using Units;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(TaskAuthoring))]
public class DismantleTaskAuthoring : MonoBehaviour
{
    public uint BaseDuration;
    public float PercentYield;
    public class MinerBaker : Baker<DismantleTaskAuthoring>
    {
        public override void Bake(DismantleTaskAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DismantleTask()
            {
                Yield = authoring.PercentYield  
            });
            AddComponent(entity, new TaskProgress
            {
                RequiredTicks = authoring.BaseDuration
            });
        }
    }
}