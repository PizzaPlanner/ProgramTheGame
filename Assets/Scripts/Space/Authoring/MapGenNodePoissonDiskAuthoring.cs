using Unity.Collections;
using Unity.Mathematics;

namespace Space.Authoring
{
    public class MapGenNodePoissonDiskAuthoring : MapGenerationNodeAuthoring<PoissonDisk>
    {
        public WeightedChild[] Children; 
        public int2 MinSize, MaxSize;
        public float MinDistance;
        protected override PoissonDisk Data
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
                return new PoissonDisk
                {
                    Children = list,
                    MinSize = MinSize,
                    MaxSize = MaxSize,
                    MinDistance = MinDistance
                };
            }
        } 
        class Baker : MapGenBaker<MapGenNodePoissonDiskAuthoring, PoissonDisk> { }      
    }
}