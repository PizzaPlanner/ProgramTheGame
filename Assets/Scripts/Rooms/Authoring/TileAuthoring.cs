using Grids;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class TileAuthoring : MonoBehaviour
{
    public Texture2D Top;
    public Texture2D Walls;
    public Texture2D Floor;
    

    class Baker : Baker<TileAuthoring>
    {
        public override void Bake(TileAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.None), new TileVisuals
            {
                Floor = authoring.Floor,
                WallSide = authoring.Walls,
                WallTop = authoring.Top,
            });
        }
    }
}