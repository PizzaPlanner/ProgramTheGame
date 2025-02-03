using Core.Input;

namespace Core
{
    public interface ICursorTool : IInputActionState
    {
        string[] IInputActionState.StateSets => new[]{"CursorTool"};
    }
}