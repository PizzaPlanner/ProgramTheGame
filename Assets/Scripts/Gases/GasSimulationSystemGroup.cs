using Unity.Entities;

public partial class GasSimulationSystemGroup : ComponentSystemGroup
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RateManager = new RateUtils.VariableRateManager(100, false);
    }
}