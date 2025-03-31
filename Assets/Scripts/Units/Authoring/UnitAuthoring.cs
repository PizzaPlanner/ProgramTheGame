using Space;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(DefaultMassAuthoring))]
public class UnitAuthoring : ItemAuthoring
{
    class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent<Unit>(e);
            AddComponent<CrewMember>(e);
            AddBuffer<Statuses>(e);
        }
    }
}