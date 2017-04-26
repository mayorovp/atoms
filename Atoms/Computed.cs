using System;
using System.Collections.Generic;
using Atoms.Core;

namespace Atoms
{
    public sealed class Computed<T> : ComputedBase<T>, IReadOnlyAtom<T>
    {
        private readonly IEqualityComparer<T> equalityComparer;
        private readonly Func<T> expr;

        public Computed(Func<T> expr, IEqualityComparer<T> equalityComparer)
        {
            this.expr = expr;
            this.equalityComparer = equalityComparer;
        }

        protected override bool OnCompare(T oldValue, T newValue) => equalityComparer.Equals(oldValue, newValue);
        protected override T OnCompute() => expr();

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
        protected override void OnChanged() => _changed?.Invoke();
    }
}