// using Core;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using Unity.Collections;
// using Unity.Entities;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using static BlueprintUtils;
//
// [UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderLast = true)]
// public partial class BlueprintMappingSystem : IdentifiableMappingSystem
// {
//     Texture2D BlueprintTexture;
//     protected override void OnCreate()
//     {
//         this.RegisterAdditionalIds = LoadBlueprints;
//         BlueprintTexture = Addressables.LoadAssetAsync<Texture2D>("BlueprintIcon").WaitForCompletion();
//     }
//
//     IEnumerable<DynamicIdentifiableDefinition> LoadBlueprints()
//     {
// #if UNITY_EDITOR
//         if(!Application.isPlaying) yield break;
// #endif  
//         if (!Directory.Exists(UserBlueprintsFolder))
//             Directory.CreateDirectory(UserBlueprintsFolder);
//
//         foreach (var bpPath in GetBlueprintFiles(UserBlueprintsFolder))
//         {
//             var def = LoadBlueprint(bpPath, SourceType.Local); 
//             if(def.Name != default)
//                 yield return def;
//         }
//         
//         foreach(var mod in Mod.AllMods)
//             if(Directory.Exists(mod.FilePath.ToString()))
//                 foreach (var bpPath in GetBlueprintFiles(mod.FilePath.ToString()))
//                 {
//                     var def = LoadBlueprint(bpPath, SourceType.Mod); 
//                     if(def.Name != default)
//                         yield return def;
//                 }
//             else
//                 Debug.LogWarning($"Mod {mod.Name} has invalid path {mod.FilePath}");
//     }
//
//     DynamicIdentifiableDefinition LoadBlueprint(string path, SourceType source)
//     {
//         var content = File.ReadAllText(path, Encoding.UTF8);
//         var poiStringNoExtension = Path.GetFileNameWithoutExtension(path);
//         var definition = LoadAsPrefab(content, poiStringNoExtension, source);
//         return definition;
//     }
//
//     protected override void OnUpdate() { }
//     
//     public DynamicIdentifiableDefinition LoadAsPrefab(string text, FixedString64Bytes key, SourceType source)
//     {
//         key = GetId(source, key.ToString());
//
//         try
//         {
//
//             var bp = EntityManager.CreateEntity(typeof(Prefab), typeof(Blueprint));
//             EntityManager.SetComponentData(bp, new Blueprint
//             {
//                 Data = BuildString(text)
//             });
//             // if (!Deserialize(text, EntityManager, out var prefab))
//             // {
//             //     Debug.LogError($"Error loading Blueprint {key}");
//             //     return default;
//             // }
//
//             return new DynamicIdentifiableDefinition
//             {
//                 Prefab = bp,
//                 Name = key,
//                 Tags = new FixedString64Bytes[] { "TagBlueprint" },
//                 IsSpawnable = true,
//                 Icon = BlueprintTexture
//             };
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"Error loading {key}: {e.Message}");
//             return default;
//         }
//     }
// }