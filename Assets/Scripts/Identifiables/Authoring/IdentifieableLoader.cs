#if UNITY_EDITOR
using Unity.Entities;
using UnityEngine;

public class IdentifieableLoader : MonoBehaviour
{
    // [SerializeField] IdentifiablesLookup TargetLookup;
    [SerializeField] GameObject[] AdditionalPrefabs;
    class Baker : Baker<IdentifieableLoader>
    {
        public override void Bake(IdentifieableLoader authoring)
        {
            var prefabs = 0;

            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("*", new[] { "Assets/Identifieables" }))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab") || path.Contains("__TEMPLATE__"))
                    continue;

                var obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj) && obj is GameObject go &&
                    go.TryGetComponent(out IdentifieableAuthoring id))
                {
                    RegisterPrefabForBaking(go);
                    GetEntity(go, TransformUsageFlags.None);
                    prefabs++;
                }
            }
            // var builder = IdentifiablesLookup.MakeBuilder(prefabs, out var arrayBuilder);
            // arrayBuilder[0].
            // lookup.BakeToBinary(builder);
            Debug.Log($"Created {prefabs} Identifieables");
            

            foreach (var prefab in authoring.AdditionalPrefabs)
            {
                RegisterPrefabForBaking(prefab);
                GetEntity(prefab, TransformUsageFlags.None);
            }
            Debug.Log($"Created {authoring.AdditionalPrefabs.Length} Prefabs");
        }
    }
}
#endif