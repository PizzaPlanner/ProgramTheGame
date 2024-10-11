using Unity.Entities;
using UnityEngine;

namespace Equipment.Authoring
{
    public class MineableAuthoring : MonoBehaviour
    {
        public class MineableAuthoringBaker : Baker<MineableAuthoring>
        {
            public override void Bake(MineableAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MineableTile());
            }
        }
    }
}