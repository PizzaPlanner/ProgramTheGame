using Core.Input;
using System;
using UnityEngine.InputSystem;

namespace InputStates
{
    public struct Player : IInputActionState
    {
        public string[] StateSets => new[] { "Navigation" };
        public InputActionMap StateFor(InputActions asset) => asset.Player;
        public bool IsActiveByDefault => true;

        public static InputAction Move => InputActions.For<Player>()["Move"];
        public static InputAction Rotate => InputActions.For<Player>()["Rotate"];
        public static InputAction Zoom => InputActions.For<Player>()["Zoom"];
        public static InputAction Pause => InputActions.For<Player>()["Pause"];
        public static InputAction Speed1 => InputActions.For<Player>()["Speed1"];
        public static InputAction Speed3 => InputActions.For<Player>()["Speed2"];
        public static InputAction Speed2 => InputActions.For<Player>()["Speed3"];
    }
}