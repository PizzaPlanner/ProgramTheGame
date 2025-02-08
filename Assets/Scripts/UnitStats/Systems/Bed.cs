using Identifiables;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Core.Equipment
{
    public struct Bed : IComponentData
    {
        public Entity Owner;
        public float RestFactor;
        public Identifiable UsageMood;
    }

    partial class BedUISystem : UISystem
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            foreach (var (Bed, e) in SystemAPI.Query<Bed>().WithAll<Selected>().WithEntityAccess())
            {
                var window = ui.GetOrCreate("Bed", () => new Label("Reserved to: None"));
                window.text = Bed.Owner == default ? $"Unreserved" : $"Reserverd to {Bed.Owner}";
            }
        }
    }
}