#if UNITY_EDITOR
using Identifiables;
using Identifiables.Authoring;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class IdentifieableAuthoring : MonoBehaviour
{
    [SerializeField] internal IdentifiableThingAsset Asset;
    public Texture2D Icon { set => Asset.Icon = value; get => Asset.Icon; }
    public Identifiable Id => Asset.Id;
    public TagAsset[] Tags => Asset.Tags;

    public class IdentifieableAuthoringBaker : Baker<IdentifieableAuthoring>
    {
        public override void Bake(IdentifieableAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            ModCollectionAuthoring.MakeIdentifiable(this, entity, authoring.Asset);
        }
    }
}
#endif