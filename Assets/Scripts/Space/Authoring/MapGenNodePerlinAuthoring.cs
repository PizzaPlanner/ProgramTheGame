using Unity.Collections;
using Unity.Mathematics;

namespace Space.Authoring
{
    public class MapGenNodePerlinAuthoring : MapGenerationNodeAuthoring<PerlinShape>
    {
        public WeightedChild[] Children; 
        public int2 MinSize, MaxSize;
        public float NoiseScale;

        protected override PerlinShape Data
        {
            get
            {
                var list = new FixedList128Bytes<WeightedNode>();
                foreach(var child in Children)
                    list.Add(new WeightedNode
                    {
                        Entity = child.Child.Id,
                        Weight = child.Weight
                    });
                return new PerlinShape
                {
                    Children = list,
                    MinSize = MinSize,
                    MaxSize = MaxSize,
                    NoiseScale = NoiseScale
                };
            }
        } 
        class Baker : MapGenBaker<MapGenNodePerlinAuthoring, PerlinShape> { }        
    }
}