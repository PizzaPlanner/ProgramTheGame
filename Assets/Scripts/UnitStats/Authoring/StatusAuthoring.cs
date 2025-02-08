using Identifiables;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(IdentifieableAuthoring))]
public class StatusAuthoring : MonoBehaviour
{
    [Tooltip("Leave at 0 for permanent status")]
    public uint BaseDuration;
    public int MoodChange;
    public Status.StackPolicy StackPolicy;
    public class Baker : Baker<StatusAuthoring>
    {
        public override void Bake(StatusAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Status
            {
                MoodChangePerStack = authoring.MoodChange,
                Duration = authoring.BaseDuration,
                AddPolicy = authoring.StackPolicy,
            });
        }
    }

    public Identifiable Id => GetComponent<IdentifieableAuthoring>().Id;
}