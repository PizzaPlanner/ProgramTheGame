using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(ChipRegistersAuthoring))]
public class ChipAuthoring : MonoBehaviour
{
    public ChipScript InitialScript;
    class ChipBaker : Baker<ChipAuthoring>
    {
        public override void Bake(ChipAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ChipState());
            AddComponent(e, new LogicChip{ManagedScript = authoring.InitialScript});
            AddBuffer<ChipInstructions>(e);
        }
    }
}