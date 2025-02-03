using Unity.Entities;
using UnityEngine.UIElements;

namespace Units
{
    public class SelectionUI : IComponentData
    {
        public VisualElement UI;

        public static void ClearFrom(Entity e, EntityManager manager)
        {
            if(!manager.HasComponent<SelectionUI>(e))
                return;
            var ui = manager.GetComponentObject<SelectionUI>(e);
            ui.UI?.RemoveFromHierarchy();
            manager.RemoveComponent<SelectionUI>(e);
        }
    }
}