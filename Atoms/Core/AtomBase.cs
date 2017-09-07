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

            if (observers.Add(derivation))
            {
                OnObserverAdded(derivation);
            }
            SubscribeObserver(derivation.DependencyStateMask);
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
            if (!IsActive && observers.Count == 0
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
            if (!IsActive && observers.Count == 0)
            {
                OnBecomeUnobserved();
            }
        }

        protected virtual void OnBecomeUnobserved() { }
        protected virtual void OnObserverAdded(IDerivation derivation) { }
        protected internal virtual bool DirtyCheck() => false;

        internal void ReportObservers(NodeState flag)
        {
            if (IsActive) Schedule();
            if ((observersStateMask & flag) == flag) return;

            observersStateMask |= flag;

            foreach (var observer in observers)
                observer.ReportDependencyStateChanged(flag);
        }

        protected void ReportObserved()
        {
            (SynchronizationContext.Current as DerivationScope)?.ReportObserved(this);
        }

        internal bool IsObserved => IsActive || observers.Count > 0 || SynchronizationContext.Current is DerivationScope;

        protected bool IsActive
        {
            get { return (state & NodeState.Active) == NodeState.Active; }
            set
            {
                if (value)
                {
                    state |= NodeState.Active;
                    if ((state & NodeState.ProbablyDirty) == NodeState.ProbablyDirty) Schedule();
                }
                else
                {
                    state &= ~NodeState.Active;
                    CheckBecomeUnobserved();
                }
            }
        }
    }
}
