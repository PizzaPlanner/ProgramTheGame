using Core.Equipment;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(StatusProviderAuthoring))]
public class BrainAuthoring : MonoBehaviour
{
    public class Baker : Baker<BrainAuthoring>
    {
        public override void Bake(BrainAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Brain() { });
        }
    }
}