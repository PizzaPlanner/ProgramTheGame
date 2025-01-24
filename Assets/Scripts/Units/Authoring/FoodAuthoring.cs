using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(ItemAuthoring))]
public class FoodAuthoring : MonoBehaviour
{
    public uint Nutrition;
    public class StomachAuthoringBaker : Baker<FoodAuthoring>
    {
        public override void Bake(FoodAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Food
            {
                Nutrition = authoring.Nutrition
            });
        }
    }
}