using Identifiables.UI;
using Unity.Entities;

namespace Items.UI
{
    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class InventoryDragDropSystem : SystemBase
    {
        static InventoryButton _dragFrom, _dragTo;

        protected override void OnCreate() { }

        protected override void OnStartRunning()
        {
            InventoryButton.OnDragDrop += OnDragDrop;
            InventoryButton.DragDropGhost = new IdentifiableButton(){name = "DragDropVisuals"};
            UISystemGroup.AddOverlay(InventoryButton.DragDropGhost);
        }

        protected override void OnUpdate()
        {
            if (InventoryButton.DragDropGhost == null)
                return;

            if (_dragFrom != null && _dragTo != null)
            {
                SystemAPI.GetSingleton<TransactionStore>().CreateBuilder
                         .Transfer(_dragFrom.Container, _dragFrom.SlotId, _dragTo.Container, _dragTo.SlotId);
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