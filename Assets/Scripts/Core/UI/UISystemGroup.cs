using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class UISystemGroup : ComponentSystemGroup
{
    public static VisualElement Root {get; private set;}
    protected override void OnUpdate()
    {
        Root ??= GameObject.FindFirstObjectByType<UIDocument>().rootVisualElement;
        if(Root == null)
            return;
        
        base.OnUpdate();
    }
}