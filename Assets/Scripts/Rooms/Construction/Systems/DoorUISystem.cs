using Tasks.UI;
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
                Add(Open = new Button(() => wantOpen = true){text = "Open"});
                Add(Auto = new Button(() => wantAuto = true){text = "Auto"});
                Add(Locked = new Button(() => wantLocked = true){text = "Locked"});
            }
        }
        
        protected override void OnUpdate()
        {
            foreach (var (door, doorState, e) in SystemAPI.Query<RefRW<Door>, EnabledRefRW<DoorStateChange>>()
                                                                  .WithAll<Selected>()
                                                                  .WithNone<UnderConstruction>()
                                                                  .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                                                                  .WithEntityAccess())
            {
                if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                    return;
                
                var window = ui.Q<DoorUI>();
                if (window == null)
                {
                    window = new DoorUI();
                    ui.Add("Door", window);
                }

                if (window.wantOpen)
                {
                    window.wantOpen = false;
                    doorState.ValueRW = true;
                    door.ValueRW.IsOpen = true;
                }
                if (window.wantLocked)
                {
                    window.wantLocked = false;
                    doorState.ValueRW = true;
                    door.ValueRW.IsOpen = false;
                }
                
                
            }
        }
    }
}