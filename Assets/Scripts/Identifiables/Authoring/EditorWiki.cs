using Identifiables.UI;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
            rootVisualElement.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/Wiki.uss").WaitForCompletion());
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
                            IdentifiablesMappingGroup.RunMapping(world);
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