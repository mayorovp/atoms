using System;
using System.Collections.Generic;

namespace Atoms
{
    public sealed class AtomRef<T> : Core.AtomBase, IReadWriteAtom<T>
    {
        private readonly IEqualityComparer<T> comparer;

        public AtomRef(IEqualityComparer<T> comparer = null)
        {
            this.comparer = comparer ?? EqualityComparer<T>.Default;
        }

        private T _value;
        public T Value
        {
            get
            {
                ReportObserved();
                return this._value;
            }

            set
            {
                if (!comparer.Equals(this._value, value))
                {
                    using (Atom.StartBatch())
                    {
                        this._value = value;
                        ReportObservers(Core.NodeState.Dirty);
                    }
                }
            }
        }

        public T Peek() => this._value;

        private Action _changed;
        public event Action Changed
        {
            add
            {
                if (_changed == null && value != null) IsWatched = true;
                _changed += value;
            }
            remove
            {
                _changed -= value;
                if (_changed == null && value != null) IsWatched = false;
            }
        }
        protected override void OnScheduledExecute() => _changed?.Invoke();
    }
}
