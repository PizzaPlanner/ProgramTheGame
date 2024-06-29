using Core.Input;
using System.Collections.Generic;

namespace Core
{
    public interface ICursorTool : IInputActionState
    {
        string[] IInputActionState.StateSets => new[]{"CursorTool"};
    }
}