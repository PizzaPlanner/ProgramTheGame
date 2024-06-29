using System;
using Unity.Entities;
using UnityEngine;

namespace Gases.Authoring
{
    public class GasEmitterAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct GasAndMoles
        {
            public Gas Gas;
            public float Moles;
        }
        
        public bool IsExhaustive;
        public GasAndMoles[] Gas;
        public float TemperatureKelvin;
        class Baker : Baker<GasEmitterAuthoring>
        {
            public override void Bake(GasEmitterAuthoring authoring)
            {
                var volume = GasVolume.Empty;
                foreach(var gas in authoring.Gas)
                    volume = volume.With(gas.Gas, gas.Moles);
                var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(e, new GasEmitter
                {
                    IsExhaustive = authoring.IsExhaustive,
                    Volume =  volume.WithTemperature(authoring.TemperatureKelvin)
                });
            }
        }
    }
}