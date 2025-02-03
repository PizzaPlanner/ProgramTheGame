using Identifiables;

namespace Core.Status
{
    public struct Hungry : IStatus { }
    public struct Starving : IStatus { }
    public struct LowOxygen : IStatus { }
    public struct Suffocating : IStatus { }

    public struct Dead : IStatus
    {
        public Identifiable Cause;
    }
}

