using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable 252

namespace Pavel.Atoms
{
    public abstract class AtomBase
    {
        private static readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly ThreadLocal<AtomBase> currentEvaluation = new ThreadLocal<AtomBase>();
        private readonly List<WeakReference<AtomBase>> parents = new List<WeakReference<AtomBase>>();
        private readonly WeakReference<AtomBase> self;

        public AtomBase() { self = new WeakReference<AtomBase>(this); }

        private object state = DIRTY; // state in [ READY, DIRTY, ManualResetEventSlim ]
        private static readonly string DIRTY = "DIRTY";
        private static readonly string READY = "READY";

        // NotifyDirty выполняется во время фазы распространения загрязнения.
        // Эта фаза работает в однопоточном режиме.
        private void NotifyDirty()
        {
            if (state == READY)
            {
                state = DIRTY;
                foreach (var parentRef in parents)
                {
                    AtomBase parent;
                    if (parentRef.TryGetTarget(out parent))
                        parent.NotifyDirty();
                }
                parents.Clear();
            }
        }

        private static readonly ConcurrentQueue<AtomBase> dirtyQueue = new ConcurrentQueue<AtomBase>();
        protected void Notify()
        {
            if (!rwlock.TryEnterWriteLock(0))
                dirtyQueue.Enqueue(this);

            try
            {
                AtomBase next = this;
                do { next.NotifyDirty(); }
                while (dirtyQueue.TryDequeue(out next));
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        protected Evaluation StartEvaluation()
        {
            var handler = EvaluationHandler();
            return new Evaluation((IDisposable)handler, handler.MoveNext() && handler.Current);
        }

        // EvaluationHandler выполняется во время фазы рассчетов
        // Эта фаза работает в многопоточном режиме
        private IEnumerator<bool> EvaluationHandler()
        {
            var parentEvaluation = currentEvaluation.Value;
            if (parentEvaluation == null) rwlock.EnterReadLock();
            currentEvaluation.Value = this;

            try
            {
                lock (parents) parents.Add(parentEvaluation.self);

                if (state == READY) yield return false;
                using (var _event = new ManualResetEventSlim())
                {
                    var oldState = Interlocked.CompareExchange(ref state, _event, DIRTY);
                    if (oldState == READY) yield return false;
                    if (oldState is ManualResetEventSlim)
                    {
                        ((ManualResetEventSlim)oldState).Wait();
                        yield return false;
                    }

                    try
                    {
                        yield return true;
                    }
                    finally
                    {
                        state = READY;
                        _event.Set();
                    }
                }
            }
            finally
            {
                currentEvaluation.Value = parentEvaluation;
                if (parentEvaluation == null) rwlock.ExitReadLock();
            }
        }

        protected struct Evaluation : IDisposable
        {
            private readonly IDisposable handler;
            private readonly bool dirty;
            internal Evaluation(IDisposable handler, bool dirty)
            {
                this.handler = handler;
                this.dirty = dirty;
            }

            public bool IsDirty { get { return dirty; } }
            public void Dispose() { handler.Dispose(); }
        }
    }
}
