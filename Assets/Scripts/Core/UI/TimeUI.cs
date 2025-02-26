using Unity.Entities;
using UnityEngine.UIElements;

namespace Core
{
    public class TimeUI : VisualElement
    {
        public float DesiredUps = -1;
        public bool WantSwitchSandbox;
        public TimeUI(bool isSandbox)
        {
            var top = new VisualElement();
            Add(top);
            top.Add(new Label{name = "Time"});
            if (isSandbox)
            {
                var sbToggle = new Toggle("Sandbox Mode"){value = true};
                sbToggle.RegisterValueChangedCallback(x => WantSwitchSandbox = true);
                top.Add(sbToggle);
            }
            var bottom = new VisualElement();
            Add(bottom);
            bottom.Add(new Button(() => DesiredUps = 0){name = "Pause", text = "II"});
            bottom.Add(new Button(() => DesiredUps = 5){name = "x1", text = "x1"});
            bottom.Add(new Button(() => DesiredUps = 10){name = "x2", text = "x2"});
            bottom.Add(new Button(() => DesiredUps = 20){name = "x4", text = "x4"});
        }

        public void Update(SimulationState state)
        {
            this.Q<Label>("Time").text = $"Tick {state.LastSimulationTick}\nUPS {(state.DeltaTime > 0 ? 1/state.DeltaTime : 0):0.#}";
            if (state.RequestedTicksPerSecond == 0)
            {
                this.Q<Button>("Pause").AddToClassList("active");
                this.Q<Button>("x1").RemoveFromClassList("active");
                this.Q<Button>("x2").RemoveFromClassList("active");
                this.Q<Button>("x4").RemoveFromClassList("active");
            }
            else if (state.RequestedTicksPerSecond == 5)
            {
                this.Q<Button>("x1").AddToClassList("active");
                this.Q<Button>("Pause").RemoveFromClassList("active");
                this.Q<Button>("x2").RemoveFromClassList("active");
                this.Q<Button>("x4").RemoveFromClassList("active");
                
            }
            else if (state.RequestedTicksPerSecond == 10)
            {
                this.Q<Button>("x2").AddToClassList("active");
                this.Q<Button>("Pause").RemoveFromClassList("active");
                this.Q<Button>("x1").RemoveFromClassList("active");
                this.Q<Button>("x4").RemoveFromClassList("active");
                
            }
            else if (state.RequestedTicksPerSecond == 20)
            {
                this.Q<Button>("x4").AddToClassList("active");
                this.Q<Button>("Pause").RemoveFromClassList("active");
                this.Q<Button>("x1").RemoveFromClassList("active");
                this.Q<Button>("x2").RemoveFromClassList("active");
                
            }
        }
    }

    partial class TimeUISystem : UISystem
    {
        TimeUI _view;
        protected override void OnCreate()
        {
            RequireForUpdate<SimulationState>();
            _view = new TimeUI(SystemAPI.HasSingleton<WorldTypes.Sandbox>());
            UISystemGroup.AddOverlay(_view);
            _view.style.display = DisplayStyle.Flex;
            _view.style.position = Position.Absolute;
        }

        protected override void OnUpdate()
        {
            ref var timeState = ref SystemAPI.GetSingletonRW<SimulationState>().ValueRW;
            if (_view.DesiredUps != -1)
            {
                timeState.RequestedTicksPerSecond = _view.DesiredUps;
                _view.DesiredUps = -1;
            }
            _view.Update(timeState);

            if (_view.WantSwitchSandbox)
            {
                ref var sandbox = ref SystemAPI.GetSingletonRW<WorldTypes.Sandbox>().ValueRW;
                sandbox.On = !sandbox.On;
                _view.WantSwitchSandbox = false;
            }
        }
    }
}