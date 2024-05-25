using Identifiables.UI;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Identifiables.Authoring
{
    public class EditorWiki : EditorWindow
    {
        [MenuItem("Window/Identifieables/Wiki")]
        static void ShowWiki()
        {
            ((EditorWindow)GetWindow<EditorWiki>()).Show();
        }

        static World _current;
        
        public void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Identifiables/UI/wiki/wiki.uss"));
            var wiki = new Wiki();
            rootVisualElement.Add(new Button(() =>
            {
                var menu = new GenericMenu();
                foreach (var world in World.All)
                {
                    menu.AddItem(new GUIContent(world.Name), _current == world,
                        () =>
                        {
                            _current = world;
                            wiki.Init(_current);
                        });
                }
                menu.ShowAsContext();
            })
            {
                name = "SelectWorld",
                text = "World Selector",
                tooltip = "Load Wiki with Data from this world",
            });
            rootVisualElement.Q<Button>().style.height = 24;
            rootVisualElement.Add(wiki);
        }
    }
}