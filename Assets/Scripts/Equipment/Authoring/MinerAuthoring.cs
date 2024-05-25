using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(CanTargetUnitsAuthoring))]
public class MinerAuthoring : MonoBehaviour
{
    public int Range;
    public int CooldownInTicks;

    public class MinerBaker : Baker<MinerAuthoring>
    {
        public override void Bake(MinerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Miner { MinerRange = authoring.Range, Cooldown = authoring.CooldownInTicks, ReadyIn  = authoring.CooldownInTicks });
        }
    }
}