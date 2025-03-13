using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Construction.Systems
{
    partial class DoorUISystem : UISystem
    {
        class DoorUI : VisualElement
        {
            public Button Open, Auto, Locked;
            
            public bool wantOpen, wantAuto, wantLocked; 

            public DoorUI()
            {
                Add(new EntityLinkButton(typeof(SpaceSuitDock)));
                Add(Open = new Button(() => wantOpen = true){text = "Open"});
                Add(Auto = new Button(() => wantAuto = true){text = "Auto"});
                Add(Locked = new Button(() => wantLocked = true){text = "Locked"});
            }
        }
        
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            foreach (var (door, e) in SystemAPI.Query<RefRW<Door>>()
                                                                  .WithAll<Selected>()
                                                                  .WithNone<Ghost>()
                                                                  .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                                                                  .WithEntityAccess())
            {
                var window = ui.Q<DoorUI>();
                if (window == null)
                {
                    window = new DoorUI();
                    ui.Add("Door", window);
                }

                if (window.wantOpen)
                {
                    window.wantOpen = false;
                    door.ValueRW.TargetState = Door.DoorState.Open;
                }
                if (window.wantLocked)
                {
                    window.wantLocked = false;
                    door.ValueRW.TargetState = Door.DoorState.Locked;
                }
                if (window.wantAuto)
                {
                    window.wantAuto = false;
                    door.ValueRW.TargetState = Door.DoorState.Closed;
                }
            }
        }
    }
}