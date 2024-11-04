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
            var elm = authoring.Element.Element.AsCompact;
            AddComponent(GetEntity(TransformUsageFlags.None), new MassData
            {
                ElementType = authoring.Element.GetComponent<IdentifieableAuthoring>().Id, 
                Mass = authoring.Mass, 
                ElementData = elm,
                Temperature = new Temperature{ Celsius = authoring.Temperature } // Must be assigned after ElementData 
            });
        }
    }
}