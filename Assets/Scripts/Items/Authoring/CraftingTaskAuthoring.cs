using Units;
using Unity.Entities;
using UnityEngine;

public class CraftingTaskAuthoring : MonoBehaviour
{
    class Baker : Baker<CraftingTaskAuthoring>
    {
        public override void Bake(CraftingTaskAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.None);
            AddComponent<TaskProgress>(self);
            AddComponent<CraftRecipeTask>(self);
        }
    }
}