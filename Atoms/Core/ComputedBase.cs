using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Atoms.Core
{
    public abstract class ComputedBase<T> : AtomBase, IDerivation, IReadOnlyAtom<T>
    {
        private HashSet<AtomBase> dependencies;
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

            using (var track = new DerivationScopeHelper(this, ref dependencies))
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

        void IDerivation.ReportStateChanged(NodeState flag)
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

        void IDerivation.ReportObserved(AtomBase atom, object tag)
        {
            CheckQueueAccess();

            if (tag == dependencies && dependencies.Add(atom))
            {
                atom.AddObserver(this);
                atom.SubscribeObserver(state);
            }
        }

        protected override void OnBecomeUnobserved()
        {
            if (dependencies != null)
            {
                foreach (var atom in dependencies)
                    atom.RemoveObserver(this);
                dependencies = null;
            }
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

        protected internal override bool DirtyCheck() => DerivationUtils.ShouldExecute(ref state, dependencies) && TrackAndCompute();

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
                if (DerivationUtils.ShouldExecute(ref state, dependencies)) TrackAndCompute();
                ReportObserved();
                return GetCachedValue();
            }
        }

        public T Peek()
        {
            CheckQueueAccess();
            CheckReentrancy();

            if (!IsObserved) return OnCompute();
            if (DerivationUtils.ShouldExecute(ref state, dependencies)) TrackAndCompute();
            return GetCachedValue();
        }

        protected override void OnScheduledExecute()
        {
            if (DerivationUtils.ShouldExecute(ref state, dependencies)) OnChanged();
        }

        protected virtual void OnChanged() { }
    }
}
