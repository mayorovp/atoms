using System.Collections.Generic;
using System.Threading;

namespace Atoms.Core
{
    public abstract class AtomBase : Node
    {
        private readonly HashSet<IDerivation> observers = new HashSet<IDerivation>();
        private NodeState observersStateMask;

        internal void AddObserver(IDerivation derivation)
        {
            CheckQueueAccess();

            var f = state & (NodeState.Dirty | NodeState.ProbablyDirty);
            if (observers.Add(derivation) && f != NodeState.None)
            {
                derivation.ReportStateChanged(f);
            }
        }
        internal void RemoveObserver(IDerivation derivation)
        {
            CheckQueueAccess();

            if (observers.Remove(derivation))
            {
                CheckBecomeUnobserved();
            }
        }
        internal void SubscribeObserver(NodeState flag)
        {
            observersStateMask &= flag;
        }

        private void CheckBecomeUnobserved()
        {
            if (!IsWatched && observers.Count == 0
                && (state & NodeState.BecomeUnobservedRequired) == NodeState.BecomeUnobservedRequired
                && (state & NodeState.BecomeUnobserved) != NodeState.BecomeUnobserved)
            {
                state |= NodeState.BecomeUnobserved;
                queue.BecomeUnobserved(this);
            }
        }

        internal void BecomeUnobserved()
        {
            state &= ~NodeState.BecomeUnobserved;
            if (!IsWatched && observers.Count == 0)
            {
                OnBecomeUnobserved();
            }
        }

        protected virtual void OnBecomeUnobserved() { }

        protected internal virtual bool DirtyCheck() => false;

        internal void ReportObservers(NodeState flag)
        {
            if (IsWatched) Schedule();
            if ((observersStateMask & flag) == flag) return;

            observersStateMask |= flag;

            foreach (var observer in observers)
                observer.ReportStateChanged(flag);
        }

        protected void ReportObserved()
        {
            (SynchronizationContext.Current as DerivationScope)?.ReportObserved(this);
        }

        internal bool IsObserved => IsWatched || observers.Count > 0 || SynchronizationContext.Current is DerivationScope;

        protected bool IsWatched
        {
            get { return (state & NodeState.Watched) == NodeState.Watched; }
            set
            {
                if (value)
                {
                    state |= NodeState.Watched;
                    if ((state & NodeState.ProbablyDirty) == NodeState.ProbablyDirty) Schedule();
                }
                else
                {
                    state &= ~NodeState.Watched;
                    CheckBecomeUnobserved();
                }
            }
        }
    }
}
