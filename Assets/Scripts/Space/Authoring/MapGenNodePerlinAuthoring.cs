using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Space.Authoring
{
    public class MapGenNodePerlinAuthoring : MapGenerationNodeAuthoring<PerlinShape>
    {
        public WeightedChild[] Children; 
        public int2 MinSize, MaxSize;
        public float NoiseScale;

        protected override BlobAssetReference<PerlinShape> Data
        {
            get
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var blob = ref builder.ConstructRoot<PerlinShape>();
                var childBuilder = builder.Allocate(ref blob.Children, Children.Length);
                for (var i = 0; i < Children.Length; i++)
                {
                    var child = Children[i];
                    childBuilder[i] = new WeightedNode
                    {
                        Entity = child.Child.Id,
                        Weight = child.Weight
                    };
                }
                blob.MinSize = MinSize;
                blob.MaxSize = MaxSize;
                blob.NoiseScale = NoiseScale;
                return builder.CreateBlobAssetReference<PerlinShape>(Allocator.Persistent);
            }
        } 
        class Baker : MapGenBaker<MapGenNodePerlinAuthoring> { }        
    }
}