using Units;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(TaskAuthoring))]
public class MiningTaskAuthoring : MonoBehaviour
{
    public class MinerBaker : Baker<MiningTaskAuthoring>
    {
        public override void Bake(MiningTaskAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<MiningTask>(entity);
            AddComponent(entity, new TaskProgress
            {
                RequiredValue = 1
            });
        }
    }
}