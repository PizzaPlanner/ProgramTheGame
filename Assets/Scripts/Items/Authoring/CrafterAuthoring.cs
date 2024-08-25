using Units;
using Unity.Entities;
using UnityEngine;

public class CrafterAuthoring : MonoBehaviour
{
    class Baker : Baker<CrafterAuthoring>
    {
        public override void Bake(CrafterAuthoring authoring)
        {
            var self = GetEntity(TransformUsageFlags.None);
            AddComponent(self, new CrafterLogic { });
            AddBuffer<CraftingQueue>(self);
            AddBuffer<Craftables>(self);
            AddBuffer<TaskProvider>(self);
            AddBuffer<Skills>(self);
            AddBuffer<ToDos>(self);
        }
    }
}