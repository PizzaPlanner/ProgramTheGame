#if UNITY_EDITOR
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IdentifiableAsset), true)]
[CanEditMultipleObjects]
public class IdentifiableAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if(targets.Length == 1)
            GUILayout.Label($"{(target as IdentifiableAsset)!.name} ({(target as IdentifiableAsset)!.Id.DebugId})");
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomEditor(typeof(IdentifieableAuthoring))]
[CanEditMultipleObjects]
public class IdentifiableAuthoringEditor : Editor
{
    Editor assetEditor;
    ModCollectionAsset assignMod;
    public override void OnInspectorGUI()
    {
        var assets = targets.OfType<IdentifieableAuthoring>().Select(x => x.Asset).ToArray();
        var missingAssets = assets.Count(x => x == null);
        if (missingAssets > 0)
        {
            foreach (var authoring in targets.OfType<IdentifieableAuthoring>())
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(authoring.gameObject);
                var asset = AssetDatabase.LoadAssetAtPath<IdentifiableThingAsset>(prefabPath) ?? CreateInstance<IdentifiableThingAsset>();
                asset.Reference = authoring;
                asset.AssignEditorValues();
                AssetDatabase.AddObjectToAsset(asset, AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                var so = new SerializedObject(authoring);
                so.FindProperty("Asset").objectReferenceValue = asset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();
            return;
        }
        Editor.CreateCachedEditor(assets, typeof(IdentifiableAssetEditor), ref assetEditor);
        assetEditor?.OnInspectorGUI();
    }
}
#endif