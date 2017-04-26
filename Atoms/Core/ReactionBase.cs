using System;
using System.Collections.Generic;
using System.Threading;

namespace Atoms.Core
{
    public abstract class ReactionBase : Node, IDerivation, IDisposable
    {
        private HashSet<AtomBase> dependencies = new HashSet<AtomBase>();

        public ReactionBase()
        {
            state |= NodeState.Dirty;
        }

        protected void Track(Action action)
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return;

            CheckQueueAccess();

            using (var track = new DerivationScopeHelper(this, ref dependencies))
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

            using (var track = new DerivationScopeHelper(this, ref dependencies))
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

        void IDerivation.ReportStateChanged(NodeState flag)
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

        void IDerivation.ReportObserved(AtomBase atom, object tag)
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return;
            CheckQueueAccess();

            if (tag == dependencies && dependencies.Add(atom))
            {
                atom.AddObserver(this);
                atom.SubscribeObserver(state);
            }
        }

        protected override sealed void OnScheduledExecute()
        {
            if ((state & NodeState.Disposed) == NodeState.Disposed) return;

            if (DerivationUtils.ShouldExecute(ref state, dependencies))
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
            foreach (var atom in dependencies)
            {
                atom.RemoveObserver(this);
            }
            dependencies.Clear();
        }

        public event Action<Exception> Error;
    }
}
