using Unity.Entities;
using UnityEngine;

namespace Core
{
    public struct SimulationState : IComponentData
    {
        public uint LastSimulationTick;
        public uint SystemVersionAtLastTick;
    }
    
    public partial class TickBasedSystemGroup : ComponentSystemGroup
    {
        public bool TickedThisFrame;
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(200, false);
            EntityManager.CreateSingleton<SimulationState>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SimulationState>());
        }

        protected override void OnUpdate()
        {
            if (!RateManager.ShouldGroupUpdate(this))
            {
                TickedThisFrame = false;
                return;
            }
            TickedThisFrame = true;
            ref var simState = ref SystemAPI.GetSingletonRW<SimulationState>().ValueRW;
            var rm = RateManager;
            RateManager = null;
            base.OnUpdate();
            simState.LastSimulationTick++;
            simState.SystemVersionAtLastTick = LastSystemVersion;
            RateManager = rm;
        }
    }
}