using Core.Grid;
using Units;
using Unity.Entities;

public class UnitAuthoring : ItemAuthoring
{
    class Baker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new Item
            {
                MaxStack = 1
            });
            AddComponent<Unit>(e);
            AddComponent<CrewMember>(e);
            AddBuffer<Statuses>(e);
        }
    }
}