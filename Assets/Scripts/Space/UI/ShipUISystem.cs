using Identifiables;
using Identifiables.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
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
                // var roomModel = SystemAPI.GetComponent<RoomModel>(baked.Grid);
                // stats.Room = roomModel.CellRoomMap[baked.Area.Min];
                var travelBtn = ui.GetOrCreateActionButton(Core.ID.TravelTask, EntityManager);
                if (travelBtn.wantCreateTask)
                {
                    UISystemGroup.ShowAsPopup(new TravelUI(EntityManager, baked.Grid, e));
                    travelBtn.wantCreateTask = false;
                }
                
            } 
        }
    }

    class TravelUI : VisualElement
    {
        public TravelUI(EntityManager mgr, Entity currentLocation, Entity controller)
        {
            Add(new Label("Select Destination"));
            using var query = mgr.CreateEntityQuery(typeof(Sector), typeof(Identifiable));
            var ids = query.ToComponentDataArray<Identifiable>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; ++i)
            {
                var btn = new IdentifiableButton(){Target = ids[i], enabledSelf = entities[i] != currentLocation};
                var target = entities[i];
                btn.RegisterCallback<MouseDownEvent>((_) =>
                {
                    var currentSector = mgr.GetComponentData<Sector>(currentLocation);
                    var toSector = mgr.GetComponentData<Sector>(target);
                    var ecb = new EntityCommandBuffer(Allocator.Temp);
                    var task = Task.Schedule(Core.ID.TravelTask, ecb, new Task
                    {
                        Target = controller,
                        Executor = controller,
                        Origin = controller
                    });
                    ecb.SetComponent(task, new TravelTask
                    {
                        ToLocation = target, 
                        FromLocation = currentLocation, 
                        From = currentSector,
                        To = toSector,
                        TakeoffPointInFromSector = mgr.GetComponentData<LocalTransform>(controller).Position
                    });
                    ecb.SetComponent(task, mgr.GetComponentData<Sector>(currentLocation));
                    ecb.Playback(mgr);
                });
                Add(btn);
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