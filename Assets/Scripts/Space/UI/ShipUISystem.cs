using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Space.Systems
{
    partial class ShipUISystem : UISystem
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            foreach (var (controller, baked, e) in 
                     SystemAPI.Query<ShipContoller, BakedIntoRoomMap>()
                              .WithAll<Selected>()
                              .WithNone<Ghost>().WithEntityAccess())
            {
                var stats = ui.GetOrCreate<ShipUI>("Ship");
                var roomModel = SystemAPI.GetComponent<RoomModel>(baked.Grid);
                stats.Room = roomModel.CellRoomMap[baked.Area.Min];
            } 
        }
    }
    
    class ShipUI : VisualElement
    {
        public ShipUI()
        {
            Add(new Label("0"));
        }
        public int Room
        {
            set
            {
                this.Q<Label>().text = $"In Room {value}";
            }
        }
    }
}