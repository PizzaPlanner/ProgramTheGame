using Unity.Entities;
using UnityEngine;

public class LocalPlayerAuthoring : MonoBehaviour
{
    private class LocalPlayerBaker : Unity.Entities.Baker<LocalPlayerAuthoring>
    {
        public override void Bake(LocalPlayerAuthoring authoring)
        {
            AddComponent<LocalPlayer>(GetEntity(TransformUsageFlags.None));
        }
    }
}