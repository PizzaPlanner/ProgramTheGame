using Identifiables.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Items.UI
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class InventoryDragDropSystem : SystemBase
    {
        static InventoryButton _dragFrom, _dragTo;

        protected override void OnCreate() { }

        protected override void OnStartRunning()
        {
            InventoryButton.OnDragDrop += OnDragDrop;
        }

        protected override void OnUpdate()
        {
            if (InventoryButton.DragDropGhost == null)
            {
                var uiDoc = GameObject.FindFirstObjectByType<UIDocument>();
                if(uiDoc == null)
                    return;
                InventoryButton.DragDropGhost = uiDoc.rootVisualElement.Q<IdentifiableButton>("DragDropVisuals");
            }
            if (_dragFrom != null && _dragTo != null)
            {
                var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged); 
                ItemTransaction.Builder.Schedule(ecb).Transfer(_dragFrom.Container, _dragFrom.SlotId, _dragTo.Container, _dragTo.SlotId).Commit();
            }
            _dragFrom = null;
            _dragTo = null;
        }

        void OnDragDrop(InventoryButton from, InventoryButton to)
        {
            _dragFrom = from;
            _dragTo = to;
        }
    }
}