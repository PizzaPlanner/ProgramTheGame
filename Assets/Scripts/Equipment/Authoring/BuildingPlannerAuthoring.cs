using Equipment;
using Unity.Entities;
using UnityEngine;

public class BuildingPlannerAuthoring : MonoBehaviour
{
    class Baker : Baker<BuildingPlannerAuthoring>
    {
        public override void Bake(BuildingPlannerAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new BuildingPlanner());
        }
    }
}