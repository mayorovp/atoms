using System;

namespace Pavel.Atoms
{
    public sealed class SourceAtom<T> : Atom<T>
    {
        private T sourceValue;
        private Exception sourceException;

        public void SetValue(T sourceValue)
        {
            this.sourceValue = sourceValue;
            this.sourceException = null;
            Notify();
        }

        public void SetException(Exception sourceException)
        {
            this.sourceException = sourceException;
            this.sourceValue = default(T);
            Notify();
        }

        protected override void Evaluate(out T value, out Exception exception)
        {
            value = sourceValue;
            exception = sourceException;
        }
    }
}
