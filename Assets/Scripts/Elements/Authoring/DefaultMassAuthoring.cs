using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class DefaultMassAuthoring : MonoBehaviour
{
    public ElementAuthoring Element;
    [Range(1, 10000)]
    public uint Mass;
    [Range(-273f, 9999f)]
    public float Temperature;
    
    class MBaker : Baker<DefaultMassAuthoring>
    {
        public override void Bake(DefaultMassAuthoring authoring)
        {
            if(authoring.Element == null)
                Debug.LogError($"No Default Element assigned on {authoring.gameObject.name}", authoring.gameObject);
            var mass = authoring.Element.ToMassData(authoring.Mass, new Temperature{ Celsius = authoring.Temperature });
            AddComponent(GetEntity(TransformUsageFlags.None), mass);
        }
    }
}