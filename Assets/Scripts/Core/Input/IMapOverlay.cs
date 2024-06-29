using Core.Input;

namespace Core
{
    public interface IMapOverlay : IInputActionState
    {
        string[] IInputActionState.StateSets => new[]{"MapOverlay"};
    }
}