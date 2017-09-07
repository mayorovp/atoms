using System;
using System.Runtime.ExceptionServices;

namespace Atoms.Core
{
    public abstract class ComputedBase<T> : AtomBase, IDerivation, IReadOnlyAtom<T>
    {
        private DerivationScope scope;
        private T cachedValue;
        private ExceptionDispatchInfo cachedException;

        public ComputedBase()
        {
            state |= NodeState.BecomeUnobservedRequired | NodeState.Dirty;
        }

        protected abstract T OnCompute();
        protected abstract bool OnCompare(T oldValue, T newValue);

        private bool TrackAndCompute()
        {
            T oldValue;
            ExceptionDispatchInfo oldException;

            using (var track = new DerivationScopeHelper(this, ref scope))
            {
                state &= ~NodeState.Dirty & ~NodeState.ProbablyDirty;
                state |= NodeState.Computing;

                oldValue = cachedValue;
                oldException = cachedException;
                try
                {
                    cachedValue = track.Execute(OnCompute);
                    cachedException = null;
                }
                catch (Exception ex)
                {
                    cachedValue = default(T);
                    cachedException = ExceptionDispatchInfo.Capture(ex);
                }

                state &= ~NodeState.Computing;
            }

            if (oldException != null || cachedException != null || !OnCompare(oldValue, cachedValue))
            {
                ReportObservers(NodeState.Dirty);
                return true;
            }

            return false;
        }

        NodeState IDerivation.DependencyStateMask => state & (NodeState.ProbablyDirty | NodeState.Dirty);
        void IDerivation.ReportDependencyStateChanged(NodeState flag)
        {
            if (flag != NodeState.None && (state & NodeState.ProbablyDirty) != NodeState.ProbablyDirty)
            {
                state |= NodeState.ProbablyDirty;
                ReportObservers(NodeState.ProbablyDirty);
            }
            if ((flag & NodeState.Dirty) == NodeState.Dirty)
            {
                state |= NodeState.Dirty;
            }
        }

        protected override void OnObserverAdded(IDerivation derivation)
        {
            if ((state & NodeState.ProbablyDirty) == NodeState.ProbablyDirty)
            {
                derivation.ReportDependencyStateChanged(NodeState.ProbablyDirty);
            }
        }

        protected override void OnBecomeUnobserved()
        {
            scope?.Finish(null);
            cachedValue = default(T);
            cachedException = null;
            state |= NodeState.Dirty;

            base.OnBecomeUnobserved();
        }

        private void CheckReentrancy()
        {
            if ((state & NodeState.Computing) == NodeState.Computing)
                throw new InvalidOperationException("Circular dependencies are not allowed");
        }

        protected internal override bool DirtyCheck() => (scope == null || scope.ShouldExecute(ref state)) && TrackAndCompute();

        private T GetCachedValue()
        {
            cachedException?.Throw();
            return cachedValue;
        }

        public T Value
        {
            get
            {
                CheckQueueAccess();
                CheckReentrancy();

                if (!IsObserved) return OnCompute();
                DirtyCheck();
                ReportObserved();
                return GetCachedValue();
            }
        }

        public T Peek()
        {
            CheckQueueAccess();
            CheckReentrancy();

            if (!IsObserved) return OnCompute();
            DirtyCheck();
            return GetCachedValue();
        }
    }
}
