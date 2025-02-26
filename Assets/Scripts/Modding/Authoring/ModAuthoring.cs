using Identifiables.Authoring;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Build;
using Unity.Entities.Content;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Core
{
    [CustomEditor(typeof(ModAuthoring))]
    class ModAuthoringEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var elm = new VisualElement();
            elm.Add(new PropertyField(serializedObject.FindProperty("modContent")));
            elm.Add(new Button(() => (target as ModAuthoring).SaveSubsceneAsMod()) { text = "Export Mod"});
            elm.Add(new Button(() => Application.OpenURL($"file://{Application.persistentDataPath}/Mods")) { text = "Open Mod Folder"});
            // elm.Add(new Button(() => (target as ModAuthoring).RebuildSubscene()) { text = "Rebuild Content"});
            return elm;
        }
    }
    
    [RequireComponent(typeof(SubScene))]
    public class ModAuthoring : MonoBehaviour
    {
        [SerializeField] internal ModCollectionAuthoring modContent;
        public Mod Definition => new Mod
        {
            Name = gameObject.name,
            Type = ModType.Editor,
            SceneHash = GetComponent<SubScene>().SceneGUID
        };

        // public void RebuildSubscene()
        // {
        //     var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        //     SceneManager.SetActiveScene(scene);
        //     
        //     var selfPrefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        //     if (string.IsNullOrWhiteSpace(selfPrefab))
        //     {
        //         Debug.LogError($"ModAuthoring must be placed on a Prefab instance. Put it in a folder and it will collect all assets inside that folder.");
        //         return;
        //     }
        //     var selfFolder = selfPrefab[..selfPrefab.LastIndexOf('/')];
        //
        //     foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t=IdentifiableAsset", new[] { selfFolder }))
        //     {
        //         var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        //         var idasset = AssetDatabase.LoadAssetAtPath<IdentifiableAsset>(path);
        //         if (idasset is IdentifiableThingAsset ita)
        //             Instantiate(ita, scene);
        //         else
        //             ToGameObject(idasset);
        //     }
        //     
        //     foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t=TextAsset", new[] { selfFolder }))
        //     {
        //         var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        //         if(!path.EndsWith("yaml"))
        //             continue;
        //         var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        //         var idasset = IdentifiableAsset.CreateInstance<IdentifiableThingAsset>();
        //         idasset.name = textAsset.name;
        //         ToGameObject(idasset).gameObject.AddComponent<BlueprintAuthoring>().DefinitionYaml = textAsset;
        //     }
        //     
        //     if(!EditorSceneManager.SaveScene(scene, selfFolder+$"/{gameObject.name}.unity"))
        //         return;
        //     EditorSceneManager.CloseScene(scene, true);
        //     GetComponent<SubScene>().SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(selfFolder+$"/{gameObject.name}.unity");
        // }

        IdentifieableAuthoring ToGameObject(IdentifiableThingAsset asset)
        {
            var idAuth = new GameObject().AddComponent<IdentifieableAuthoring>();
            idAuth.gameObject.name = asset.name;
            var ser = new SerializedObject(idAuth);
            ser.Update();
            ser.FindProperty("Asset").objectReferenceValue = asset;
            ser.ApplyModifiedPropertiesWithoutUndo();
            return idAuth;
        }
            
        public void SaveSubsceneAsMod()
        {
            var modDef = Definition;
            var subSceneGuids = new HashSet<Unity.Entities.Hash128>();
            var playerGuid = DotsGlobalSettings.Instance.GetClientGUID();
            var guid = GetComponent<SubScene>().SceneGUID;
            subSceneGuids.Add(guid);
            var modsFolder = Application.persistentDataPath + "/Mods";
            var modPath = $"{modsFolder}/{modDef.Name}";
            RemoteContentCatalogBuildUtility.BuildContent(subSceneGuids, playerGuid, BuildTarget.StandaloneWindows64, $"{modPath}");
            var modJson = new ModJsonFormat
            {
                hash = modDef.SceneHash.ToString(),
                name = modDef.Name.ToString(),
            };
            var json = JsonUtility.ToJson(modJson, true);
            File.WriteAllText($"{modPath}/mod.json", json);
            Debug.Log($"Created Mod {modDef.Name} in {modsFolder}");
        }
    }
}