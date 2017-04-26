using System;
using System.Collections.Generic;
using System.Threading;

namespace Atoms.Core
{
    public sealed class NodeQueue
    {
        /* STATIC MEMBERS */
        [ThreadStatic]
        private static NodeQueue _current;
        public static NodeQueue Current => LazyInitializer.EnsureInitialized(ref _current, CreateQueue);
        private static NodeQueue CreateQueue() => new NodeQueue();

        private NodeQueue() { }

        /* INSTANCE MEMBERS */
        private readonly Queue<Node> queue = new Queue<Node>();
        private readonly Queue<AtomBase> becomeUnobservedQueue = new Queue<AtomBase>();
        private int batchLevel;

        internal void Schedule(Node node)
        {
            queue.Enqueue(node);
            if (batchLevel == 0)
            {
                StartBatch();
                EndBatch();
            }
        }
        internal void BecomeUnobserved(AtomBase atom) => becomeUnobservedQueue.Enqueue(atom);

        private void RunQueue()
        {
            if (SynchronizationContext.Current is DerivationScope) throw new InvalidOperationException("Cannot run queue while tracking dependencies");

            do
            {
                while (queue.Count > 0)
                {
                    try { queue.Dequeue().ScheduledExecute(); }
                    catch (Exception ex) when (Error != null) { Error?.Invoke(ex); }
                }
                while (becomeUnobservedQueue.Count > 0) becomeUnobservedQueue.Dequeue().BecomeUnobserved();
            } while (queue.Count > 0);
        }

        internal void StartBatch()
        {
            batchLevel++;
        }

        internal void EndBatch()
        {
            try
            {
                if (batchLevel == 1) RunQueue();
            }
            finally
            {
                batchLevel--;
            }
        }

        public event Action<Exception> Error;

        internal static void Clear() // for tests only
        {
            _current = null;
        }
    }
}
