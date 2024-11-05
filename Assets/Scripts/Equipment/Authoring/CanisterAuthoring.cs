using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class CanisterAuthoring : MonoBehaviour
    {
        public ElementAuthoring Content;
        public float TemperatureC;
        public uint MassKg;
        
        class TheBaker : Baker<CanisterAuthoring>
        {
            public override void Bake(CanisterAuthoring authoring)
            {
                var self = GetEntity(TransformUsageFlags.None);
                AddComponent<ContainerValve>(self);
                var contents = AddBuffer<MatterContainerInitializer>(self);
                if (authoring.Content != null)
                {
                    contents.Add(new MatterContainerInitializer
                    {
                        Mass = authoring.Content.ToMassData(authoring.MassKg * 1000u, new Temperature{Celsius = authoring.TemperatureC})
                    });
                }
            }
        }
    }
}