using Identifiables;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(IdentifieableAuthoring))]
public class StatusAuthoring : MonoBehaviour
{
    [RestrictTo(typeof(IStatus))]
    public TypeWrapper StatusType;
    public class Baker : Baker<StatusAuthoring>
    {
        public override void Bake(StatusAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<Status>(entity);
            AddComponent(entity, TypeManager.GetType(authoring.StatusType));
        }
    }

    public Identifiable Id => GetComponent<IdentifieableAuthoring>().Id;
}