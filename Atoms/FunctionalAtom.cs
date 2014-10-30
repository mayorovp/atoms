using System;

namespace Pavel.Atoms
{
    public sealed class FunctionalAtom<T> : DependentAtom<T>
    {
        private readonly Func<T> func;
        public FunctionalAtom(Func<T> func) { this.func = func; }
        protected override T Evaluate() { return func(); }
    }
}
