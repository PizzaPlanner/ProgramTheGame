using Core.Equipment;
using Unity.Entities;
using UnityEngine;

public class MoodAuthoring : MonoBehaviour
{
    public ushort Capacity;
    public int MaxChangePerTick;
    public class Baker : Baker<MoodAuthoring>
    {
        public override void Bake(MoodAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Mood
            {
                Capacity = authoring.Capacity,
                Fullness = 0,
                MaxChangePerTick = authoring.MaxChangePerTick
            });
        }
    }
}