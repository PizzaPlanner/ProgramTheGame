using Unity.Entities;
using UnityEngine;

namespace Core
{
    public partial class TickBasedSystemGroup : ComponentSystemGroup
    {
        public uint LastTick;
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(200, false);
        }

        protected override void OnUpdate()
        {
            if (!RateManager.ShouldGroupUpdate(this))
                return;
            var rm = RateManager;
            RateManager = null;
            base.OnUpdate();
            LastTick = LastSystemVersion;
            RateManager = rm;
        }
    }
}