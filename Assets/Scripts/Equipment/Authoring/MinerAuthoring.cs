using Unity.Entities;
using UnityEngine;

public class MinerAuthoring : MonoBehaviour
{
    public int Range;

    public class MinerBaker : Baker<MinerAuthoring>
    {
        public override void Bake(MinerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Miner { Range = authoring.Range });
        }
    }
}