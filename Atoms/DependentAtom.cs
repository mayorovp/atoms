using System;

namespace Pavel.Atoms
{
    public abstract class DependentAtom<T> : Atom<T>
    {
        protected abstract T Evaluate();

        protected sealed override void Evaluate(out T value, out Exception exception)
        {
            try { value = Evaluate(); exception = null; }
            catch (Exception ex) { value = default(T); exception = ex; }
        }
    }
}
