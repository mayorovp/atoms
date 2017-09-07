using System;

namespace Atoms.Core
{
    [Flags]
    public enum NodeState
    {
        None = 0,
        Scheduled = 1,
        Dirty = 2,
        ProbablyDirty = 4,
        BecomeUnobserved = 8,
        BecomeUnobservedRequired = 16,
        Computing = 32,
        Disposed = 64,
        Active = 128,
    }
}
