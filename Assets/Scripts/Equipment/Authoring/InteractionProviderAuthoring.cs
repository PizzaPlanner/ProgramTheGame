using Identifiables;
using Unity.Entities;
using UnityEngine;

public class InteractionProviderAuthoring : MonoBehaviour
{
    public IdentifiableAsset[] Values;
    
    class Baker : Baker<InteractionProviderAuthoring>
    {
        public override void Bake(InteractionProviderAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var statuses = AddBuffer<InteractionProvider>(e);
            foreach(var status in authoring.Values)
                statuses.Add(new InteractionProvider
                {
                    Interaction = status.Id
                });
        }
    }
}