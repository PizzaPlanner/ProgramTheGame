using Space.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Space.Authoring
{
    public class GalaxyAuthoring : MonoBehaviour
    {
        public WeightedChild[] Children;
        class Baker : Baker<GalaxyAuthoring>
        {
            public override void Bake(GalaxyAuthoring authoring)
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var blob = ref builder.ConstructRoot<GalaxyTypeDef>();
                var childBuilder = builder.Allocate(ref blob.Children, authoring.Children.Length);
                for (var i = 0; i < authoring.Children.Length; i++)
                {
                    var child = authoring.Children[i];
                    childBuilder[i] = new WeightedNode
                    {
                        Entity = child.Child?.Id ?? default,
                        Weight = child.Weight
                    };
                }
                
                var blobRef = builder.CreateBlobAssetReference<GalaxyTypeDef>(Allocator.Persistent);
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(e, new GalaxyType()
                {
                    Generator = blobRef
                });
                AddBlobAsset(ref blobRef, out _);
            }
        }
    }
}