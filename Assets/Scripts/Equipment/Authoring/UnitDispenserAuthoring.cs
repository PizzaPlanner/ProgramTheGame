using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class UnitDispenserAuthoring : MonoBehaviour
    {
        public UnitAuthoring UnitType;
        public TaskAuthoring DispensionTask;
        public class AuthoringBaker : Baker<UnitDispenserAuthoring>
        {
            public override void Bake(UnitDispenserAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitDispenser
                {
                    Unit = authoring.UnitType.GetComponent<IdentifieableAuthoring>().Id,
                    TaskPrefab = authoring.DispensionTask.GetComponent<IdentifieableAuthoring>().Id
                });
            }
        }
    }
}