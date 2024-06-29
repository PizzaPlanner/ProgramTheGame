using Core.Input;
using UnityEngine.InputSystem;

public partial class InputActions
{
    internal static InputActions Instance;
    
    public static InputActionMap For<T>()
        where T: struct, IInputActionState
    {
            return default(T).StateFor(Instance);
    }
}