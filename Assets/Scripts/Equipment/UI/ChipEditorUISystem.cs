using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

public struct EditChipUI : IComponentData { }

[UpdateInGroup(typeof(UISystemGroup))]
public partial class ChipEditorUISystem : SystemBase
{
    ChipEditorElement _editor;

    protected override void OnCreate()
    {
        RequireForUpdate<EditChipUI>();
        EntityManager.CreateEntity(typeof(EditChipUI));
    }

    protected override void OnStartRunning()
    {
        if (_editor == null)
        {
            _editor = new ChipEditorElement();
            _editor.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/ChipEditor.uss").WaitForCompletion());
        }
        if(_editor.parent == null)
            UISystemGroup.Root.Add(_editor);
    }

    protected override void OnStopRunning()
    {
        _editor.RemoveFromHierarchy();
    }

    protected override void OnUpdate() { }
}