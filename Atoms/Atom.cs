using System;
using System.Collections.Generic;
using System.Threading;

namespace Pavel.Atoms
{
    public abstract class Atom : AtomBase
    {
        public abstract object GetResult();
        public abstract Exception Exception { get; }

        private readonly List<Tuple<SynchronizationContext, Action>> changed = new List<Tuple<SynchronizationContext, Action>>();

        public event Action Changed
        {
            add { lock (changed) changed.Add(new Tuple<SynchronizationContext, Action>(SynchronizationContext.Current, value)); }
            remove { lock (changed) changed.Remove(new Tuple<SynchronizationContext, Action>(SynchronizationContext.Current, value)); }
        }

        protected override void NotifyEvents()
        {
            lock (changed)
                foreach (var handler in changed)
                    (handler.Item1 ?? new SynchronizationContext()).Post(_ => handler.Item2(), null);
        }
    }

    public abstract class Atom<T> : Atom
    {
        private SynchronizationContext sync;
        private T value;
        private Exception exception;

        protected abstract void Evaluate(out T value, out Exception exception);

        protected sealed override bool Update()
        {
            var oldValue = value;
            var oldException = exception;
            Evaluate(out value, out exception);
            sync = SynchronizationContext.Current;
            return (oldException == null) != (exception == null)
                || exception == null && CompareValues(oldValue, value)
                || exception != null && CompareExceptions(oldException, exception);
        }

        protected virtual bool CompareValues(T oldValue, T newValue)
        {
            return object.Equals(oldValue, newValue);
        }

        protected virtual bool CompareExceptions(Exception oldException, Exception newException)
        {
            return object.Equals(oldException.GetType(), newException.GetType())
                && object.Equals(oldException.Message, newException.Message)
                && object.Equals(oldException.StackTrace, newException.StackTrace);
        }

        public sealed override object GetResult() { return Value; }
        public T Value
        {
            get
            {
                using (var e = StartEvaluation())
                    if (exception != null)
                        throw new AggregateException(exception);
                    else
                        return value;
            }
        }

        public sealed override Exception Exception
        {
            get
            {
                using (var e = StartEvaluation())
                    return exception;
            }
        }

        public bool TryGetValue(out T value)
        {
            using (var e = StartEvaluation())
            {
                value = this.value;
                return exception == null;
            }
        }

        public void Read(out T value, out Exception exception)
        {
            using (var e = StartEvaluation())
            {
                value = this.value;
                exception = this.exception;
            }
        }
    }
}
