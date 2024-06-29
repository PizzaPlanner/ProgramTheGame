#if UNITY_EDITOR
using Identifiables;
using Identifiables.Authoring;
using Modding.API.Tags;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class IdentifieableAuthoring : MonoBehaviour
{
    [SerializeField][HideInInspector]ulong ItemId;
    
    public TagAuthoring[] Tags;
    public Texture2D Icon;
    public Identifieable Id => Identifieable.FromGuid(ItemId);

    private void OnValidate()
    {
        var prefabRootPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this);

        var guid = UnityEditor.AssetDatabase.AssetPathToGUID(prefabRootPath);
        if (guid.Length < 16) return;
        ItemId = ulong.Parse(guid.Substring(0,16),System.Globalization.NumberStyles.HexNumber);
        EditorGUIUtility.SetIconForObject(gameObject, Icon);
    }

    public class Baker : Baker<IdentifieableAuthoring>
    {
        public override void Bake(IdentifieableAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, Identifieable.FromGuid(authoring.ItemId));
            if (!PrefabUtility.IsPartOfPrefabInstance(authoring))
            {
                AddComponent(e, new PreIdentifiable
                {
                    // Id = authoring.Id,
                    Name = authoring.gameObject.name,
                    Icon = authoring.Icon,
                    AssetGuid = authoring.ItemId
                });
                var tags = AddBuffer<Tags>(e);
                foreach(var tag in authoring.Tags)
                    tags.Add(new Tags
                    {
                        TagId = tag.GetComponent<IdentifieableAuthoring>().ItemId
                    });
            }
        }
    }
}
#endif