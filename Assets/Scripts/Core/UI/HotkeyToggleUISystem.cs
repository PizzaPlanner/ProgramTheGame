using Core;
using Core.Input;
using UnityEngine.UIElements;

public abstract partial class HotkeyToggleUISystem<TWindow, TTool> : UISystem
    where TWindow: VisualElement
    where TTool: unmanaged, IInputActionState
{
    protected TWindow _window;
    protected abstract TWindow MakeWindow();
    protected abstract string MenuEntry { get; }
    
    protected override void OnCreate()
    {
        RequireForUpdate<TTool>();
        UISystemGroup.AddMenuEntry(MenuEntry, () => World.GetExistingSystemManaged<InputActionsSystem>().Toggle(default(TTool)));
    }
    protected override void OnStartRunning()
    {
        _window ??= MakeWindow();
        UISystemGroup.AddOverlay(_window);
        UISystemGroup.Show(_window);
    }

    protected override void OnStopRunning()
    {
        UISystemGroup.Hide(_window);
    }
}