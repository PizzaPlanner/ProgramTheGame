#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class IdentifieableAuthoring : MonoBehaviour
{
    [SerializeField][HideInInspector]ulong ItemId;

    public Identifieable Id => new Identifieable(ItemId);
    
    public string[] Tags;
    public Texture2D Icon;

    private void OnValidate()
    {
        var prefabRootPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);

        var guid = UnityEditor.AssetDatabase.AssetPathToGUID(prefabRootPath);
        if (guid.Length < 16) return;
        ItemId = ulong.Parse(guid.Substring(0,16),System.Globalization.NumberStyles.HexNumber);
        EditorGUIUtility.SetIconForObject(gameObject, Icon);
    }

    public Identifieable GetSubIdentifiable(int index)
    {
        var current = Id;
        var hash = math.hash(new uint2(current.ItemId, (uint)index));
        return new Identifieable(hash);
    }

    public class Baker : Baker<IdentifieableAuthoring>
    {
        public override void Bake(IdentifieableAuthoring authoring)
        {
            
            var e = GetEntity(TransformUsageFlags.None);
            if (!PrefabUtility.IsPartOfPrefabInstance(authoring))
            {
                AddComponent(e, new PreIdentifiable
                {
                    Id = authoring.Id,
                    Tags = IdTagManager.Bake(authoring.Tags),
                    Name = authoring.gameObject.name,
                    Icon = authoring.Icon,
                    Origin = "core"
                });
            } else
                AddComponent(e, authoring.Id);
        }
    }
}
#endif