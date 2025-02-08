using Identifiables;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(ItemAuthoring))]
public class FoodAuthoring : MonoBehaviour
{
    public uint Nutrition;
    public uint NutritionPerTick;
    public IdentifieableAuthoring MoodStatus;
    public class StomachAuthoringBaker : Baker<FoodAuthoring>
    {
        public override void Bake(FoodAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Food
            {
                Nutrition = authoring.Nutrition,
                NutritionPerTick = authoring.NutritionPerTick,
                MoodStatus = authoring.MoodStatus?.Id ?? default
            });
        }
    }
}