using System;
using System.Collections.Generic;
using System.Threading;

namespace Atoms.Core
{
    public abstract class ReactionBase : Node, IDerivation, IDisposable
    {
        private DerivationScope scope;

        public ReactionBase()
        {
            state |= NodeState.Dirty;
        }

        protected void Track(Action action)
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return;

            CheckQueueAccess();

            using (var track = new DerivationScopeHelper(this, ref scope))
            {
                state &= ~NodeState.Dirty & ~NodeState.ProbablyDirty;
                state |= NodeState.Computing;
                try
                {
                    track.Execute(action);
                }
                catch (Exception ex) when (Error != null) { Error?.Invoke(ex); }
                finally
                {
                    state &= ~NodeState.Computing;
                }
            }
        }

        protected T Track<T>(Func<T> action)
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return default(T);

            CheckQueueAccess();

            using (var track = new DerivationScopeHelper(this, ref scope))
            {
                state &= ~NodeState.Dirty & ~NodeState.ProbablyDirty;
                state |= NodeState.Computing;
                try
                {
                    return track.Execute(action);
                }
                catch (Exception ex) when (Error != null) { Error?.Invoke(ex); return default(T); }
                finally
                {
                    state &= ~NodeState.Computing;
                }
            }
        }

        NodeState IDerivation.DependencyStateMask => state & (NodeState.ProbablyDirty | NodeState.Dirty);
        void IDerivation.ReportDependencyStateChanged(NodeState flag)
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return;

            if (flag != NodeState.None)
            {
                state |= NodeState.ProbablyDirty;
                Schedule();
            }
            if ((flag & NodeState.Dirty) == NodeState.Dirty)
            {
                state |= NodeState.Dirty;
            }
        }

        protected override sealed void OnScheduledExecute()
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return;

            if (scope == null || scope.ShouldExecute(ref state))
            {
                try { Invalidate(); }
                catch (Exception ex) when (Error != null) { Error?.Invoke(ex); }
            }

            base.OnScheduledExecute();
        }

        protected abstract void Invalidate();

        public void Dispose()
        {
            CheckQueueAccess();
            state |= NodeState.Disposed;
            scope?.Finish(null);
        }

        public event Action<Exception> Error;
    }
}
