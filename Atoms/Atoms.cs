using System;

namespace Pavel.Atoms
{
    public static class Atoms
    {
        public static SourceAtom<T> Source<T>(T initialValue)
        {
            var atom = new SourceAtom<T>();
            atom.SetValue(initialValue);
            return atom;
        }

        public static SourceAtom<T> SourceException<T>(Exception initialException)
        {
            var atom = new SourceAtom<T>();
            atom.SetException(initialException);
            return atom;
        }

        public static Atom<T> Functional<T>(Func<T> func)
        {
            return new FunctionalAtom<T>(func);
        }
    }
}
