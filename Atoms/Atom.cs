using System;

namespace Pavel.Atoms
{
    public abstract class Atom : AtomBase
    {
        public abstract object GetResult();
        public abstract Exception Exception { get; }
    }

    public abstract class Atom<T> : Atom
    {
        private T value;
        private Exception exception;

        protected abstract void Evaluate(out T value, out Exception exception);

        public sealed override object GetResult() { return Value; }
        public T Value
        {
            get
            {
                using (var e = StartEvaluation())
                {
                    if (e.IsDirty) Evaluate(out value, out exception);

                    if (exception != null)
                        throw new AggregateException(exception);
                    return value;
                }
            }
        }

        public sealed override Exception Exception
        {
            get
            {
                using (var e = StartEvaluation())
                {
                    if (e.IsDirty) Evaluate(out value, out exception);
                    return exception;
                }
            }
        }

        public bool TryGetValue(out T value)
        {
            using (var e = StartEvaluation())
            {
                if (e.IsDirty) Evaluate(out value, out exception);
                value = this.value;
                return exception == null;
            }
        }

        public void Read(out T value, out Exception exception)
        {
            using (var e = StartEvaluation())
            {
                if (e.IsDirty) Evaluate(out value, out exception);
                value = this.value;
                exception = this.exception;
            }
        }
    }
}
