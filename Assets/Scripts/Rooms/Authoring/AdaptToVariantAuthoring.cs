using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class AdaptToVariantAuthoring : MonoBehaviour
{
    public BuildingVariantAuthoring[] Variants;
    class ATVBaker : Baker<AdaptToVariantAuthoring>
    {
        public override void Bake(AdaptToVariantAuthoring authoring)
        {
            var b = AddBuffer<AdaptToVariant>(GetEntity(TransformUsageFlags.Dynamic));
            foreach(var v in authoring.Variants)
                b.Add(new AdaptToVariant
                {
                    Element = v.Element.GetComponent<IdentifieableAuthoring>().Id,
                    MaterialOverride = v.OverrideMaterial
                });
        }
    }
}