using Identifiables.UI;
using UnityEngine.UIElements;
//
// [UpdateInGroup(typeof(UISystemGroup))]
// partial class GasVolumeUISystem : SystemBase
// {
//     protected override void OnUpdate()
//     {
//         var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
//                            .CreateCommandBuffer(World.Unmanaged);
//         foreach (var (volume, e) in SystemAPI.Query<MassVolume>().WithAll<Selected>().WithEntityAccess())
//         {
//             if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
//                 return;
//             
//             var window = ui.Q<GasVolumeUI>();
//             if (window == null)
//             {
//                 window = new GasVolumeUI();
//                 ui.Add("GasVolume", window);
//             }
//             window.Volume = volume;
//         }
//     }
//
// }

public class MassVolumeUI : VisualElement
{
    Label Capacity;
    Label Pressure;
    Label Temperature;
    IdentifiableButton[] Moles;

    public MassHeader Volume
    {
        set
        {
            // Capacity.text = $"Capacity: {1 / value.VolumeCapacityInverse}L";
            Pressure.text = $"Pressure: {(value.IsSolidBlock ? "-" : value.Pressure)}";
            Temperature.text = $"Temperature: {value.TemperatureCelsius}C";
            var container = this.Q("MolesContainer");
            container.Clear();
            foreach (var gas in value.Masses)
            {
                var btn = new IdentifiableButton
                {
                    name = gas.ElementType.DefaultName.Value,
                    Target = gas.ElementType
                };
                container.Add(btn);
                btn.Add(new Label($"{gas.Mass:N0}\n{gas.EnergyPerMass:N0}"));
            }
        }
    }

    public MassVolumeUI()
    {
        Add(Capacity = new Label());
        Add(Pressure = new Label());
        Add(Temperature = new Label());
        var molesContainer = new VisualElement()
        {
            name = "MolesContainer"
        };
        Add(molesContainer);
    }
}