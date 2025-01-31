#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(ModCollectionAsset))]
public class ModCollectionEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var e = new VisualElement();
        e.Add(new PropertyField(serializedObject.FindProperty("Icon")));
        e.Add(new PropertyField(serializedObject.FindProperty("Identifiables")){enabledSelf = false});
        e.Add(new Button(CollectIds){text = "Collect mod content"});
        return e;
    }

    void CollectIds()
    {
        serializedObject.Update();
        var selfFolder = AssetDatabase.GetAssetPath(target);
        selfFolder = selfFolder[..selfFolder.LastIndexOf('/')];
        
        var ids = serializedObject.FindProperty("Identifiables");
        ids.arraySize = 0;
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t=IdentifiableAsset", new[] { selfFolder }))
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var idasset = AssetDatabase.LoadAssetAtPath<IdentifiableAsset>(path);
            if(idasset is ModCollectionAsset)
                continue;
            var i = ids.arraySize;
            ids.InsertArrayElementAtIndex(i);
            ids.GetArrayElementAtIndex(i).objectReferenceValue = idasset; 
        }
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(serializedObject.targetObject);
        GenerateBoilerplate(target, selfFolder, ids);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    void GenerateBoilerplate(Object mod, string selfFolder, SerializedProperty content)
    {
        var genFolder = Directory.CreateDirectory(Path.Combine(selfFolder, "Generated"));
        File.WriteAllText(Path.Combine(genFolder.FullName, $"Mod.{mod.name}.asmdef"), $@"{{
    ""name"": ""Mod.{mod.name}"",
    ""rootNamespace"": ""{mod.name}"",
    ""references"": [
        ""Unity.Collections"",
        ""Identifiables""
    ],
    ""noEngineReferences"": true
}}
");
        File.WriteAllText(Path.Combine(genFolder.FullName, $"IDs.gen.cs") ,$@"
using Identifiables;

namespace {mod.name}
{{
    public static class ID
    {{
{GetIds()}
    }}
}}");
        
        File.WriteAllText(Path.Combine(genFolder.FullName, $"Tags.gen.cs") ,$@"
using Identifiables;

namespace {mod.name}
{{
    public static class Tags
    {{
{GetTags()}
    }}
}}");
        return;

        string GetIds()
        {
            var res = "";
            for (int i = 0; i < content.arraySize; i++)
            {
                var n = content.GetArrayElementAtIndex(i).objectReferenceValue.name;
                res += $"       public static readonly Identifiable {n} = Identifiable.FromName(\"{n}\");\n";
            }
            return res;
        }
        
        string GetTags()
        {
            var res = "";
            for (int i = 0; i < content.arraySize; i++)
            {
                var obj = content.GetArrayElementAtIndex(i).objectReferenceValue;
                if(obj is TagAsset tag)
                    res += $"       public static readonly Tag {tag.name} = Tag.FromName(\"{tag.name}\");\n";
            }
            return res;
        }
    }
}
#endif