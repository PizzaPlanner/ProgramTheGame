using Unity.Entities;
using UnityEngine;

public class ChipRegistersAuthoring : MonoBehaviour
{
    class ChipBaker : Baker<ChipRegistersAuthoring>
    {
        public override void Bake(ChipRegistersAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ChipRegisters());
        }
    }
}