using Unity.Entities;

namespace Core
{
    public partial class TickBasedSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new RateUtils.VariableRateManager(200, false);
        }
    }
}