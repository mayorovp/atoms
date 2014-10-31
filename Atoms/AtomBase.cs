﻿using System;
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
        private List<WeakReference<AtomBase>> childs = new List<WeakReference<AtomBase>>();
        private readonly WeakReference<AtomBase> self;

        public AtomBase() { self = new WeakReference<AtomBase>(this); }

        private static readonly string DIRTY = "DIRTY";
        private static readonly string CHANGED = "CHANGED";
        private static readonly string READY = "READY";
        private object state = DIRTY; // state in [ READY, DIRTY, CHANGED, StateGuard ]
        private long generation;

        // NotifyDirty выполняется во время фазы распространения загрязнения.
        // Эта фаза работает в однопоточном режиме.
        private void NotifyDirty(object newState)
        {
            if (state == READY)
            {
                state = newState;
                foreach (var parentRef in parents)
                {
                    AtomBase parent;
                    if (parentRef.TryGetTarget(out parent))
                        parent.NotifyDirty(DIRTY);
                }
                parents.Clear();

                NotifyEvents();
            }
        }

        private static readonly ConcurrentQueue<AtomBase> dirtyQueue = new ConcurrentQueue<AtomBase>();
        private static long currentGeneration;
        protected void Notify()
        {
            if (!rwlock.TryEnterWriteLock(0))
                dirtyQueue.Enqueue(this);

            try
            {
                currentGeneration++;

                AtomBase next = this;
                do { next.NotifyDirty(CHANGED); }
                while (dirtyQueue.TryDequeue(out next));
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        protected virtual void NotifyEvents() { }

        protected abstract bool Update();

        protected Evaluation StartEvaluation(bool registerAsChild = true)
        {
            var handler = EvaluationHandler(registerAsChild);
            handler.MoveNext();
            return new Evaluation(handler);
        }

        // EvaluationHandler выполняется во время фазы рассчетов
        // Эта фаза работает в многопоточном режиме
        private IEnumerator<bool> EvaluationHandler(bool registerAsChild)
        {
            var parentEvaluation = currentEvaluation.Value;
            if (parentEvaluation == null) rwlock.EnterReadLock();
            currentEvaluation.Value = this;

            try
            {
                lock (parents) parents.Add(parentEvaluation.self);
                if (registerAsChild) parentEvaluation.childs.Add(self);

                if (state == READY) yield return false;
                using (var _guard = new StateGuard())
                {
                    var oldState = state == DIRTY
                        ? Interlocked.CompareExchange(ref state, _guard, DIRTY)
                        : Interlocked.CompareExchange(ref state, _guard, CHANGED);
                    if (oldState == READY) yield return false;
                    else if (oldState is StateGuard)
                    {
                        ((StateGuard)oldState).Wait();
                        yield return false;
                    }
                    else
                    {
                        bool changed;
                        if (oldState == CHANGED)
                            changed = true;
                        else
                        {
                            changed = false;
                            foreach (var childRef in childs)
                            {
                                AtomBase child;
                                if (childRef.TryGetTarget(out child))
                                {
                                    // Чтобы определить факт изменения дочерней записи, надо сначала вычислить ее
                                    child.StartEvaluation(false).Dispose();
                                    if (child.generation > generation)
                                        changed = true;
                                }
                            }
                        }
                        if (changed)
                        {
                            childs.Clear(); // Строка ниже заного заполнит список
                            if (Update())
                                generation = currentGeneration;
                        }
                    }

                    state = READY;
                }

                // Во время выполнения yield удерживается блокировка rwlock - это дает вызывающему коду возможность безопасно прочитать свое состояние
                // Поэтому все ветви кода должны заканчиваться оператором yield
                yield return true;
            }
            finally
            {
                if (currentEvaluation.Value != this) throw new InvalidOperationException("Переключение потока во время выполнения EvaluationHandler");
                currentEvaluation.Value = parentEvaluation;
                if (parentEvaluation == null) rwlock.ExitReadLock();
            }
        }

        protected struct Evaluation : IDisposable
        {
            private readonly IEnumerator<bool> handler;
            internal Evaluation(IEnumerator<bool> handler) { this.handler = handler; }
            public void Dispose() { ((IDisposable)handler).Dispose(); }
        }
    }
}
