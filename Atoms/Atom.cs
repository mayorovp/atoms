using System;
using System.Threading;

namespace Atoms
{
    public sealed partial class Atom : Core.AtomBase
    {
        public bool ReportObservedStatus
        {
            get { return (state & Core.NodeState.BecomeUnobservedRequired) == Core.NodeState.BecomeUnobservedRequired; }
            set
            {
                if (value)
                    state |= Core.NodeState.BecomeUnobservedRequired;
                else
                    state &= ~Core.NodeState.BecomeUnobservedRequired;
            }
        }

        private bool _observed;
        public new void ReportObserved()
        {
            if (!(SynchronizationContext.Current is Core.DerivationScope)) return;

            if (!_observed)
            {
                _observed = true;
                BecomeObserved?.Invoke();
            }

            base.ReportObserved();
        }

        public event Action BecomeObserved;
        public new event Action BecomeUnobserved;

        protected override void OnBecomeUnobserved()
        {
            _observed = false;
            BecomeUnobserved?.Invoke();
        }

        public void ReportProbablyDirty() => ReportObservers(Core.NodeState.ProbablyDirty);
        public void ReportDirty() => ReportObservers(Core.NodeState.Dirty);

        public event Func<bool> DirtyChecking;
        protected internal override bool DirtyCheck() => DirtyChecking?.Invoke() ?? false;
    }
}
