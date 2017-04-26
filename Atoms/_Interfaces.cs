using System;

namespace Atoms
{
    public interface IReadOnlyAtom<out T>
    {
        T Value { get; }
        T Peek();
    }

    public interface IReadWriteAtom<T> : IReadOnlyAtom<T>
    {
        new T Value { get; set; }
    }
}
