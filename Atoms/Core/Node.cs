using System;

namespace Atoms.Core
{
    public abstract class Node
    {
        protected readonly NodeQueue queue = NodeQueue.Current;
        internal NodeState state;

        public NodeState State => state;

        protected void CheckQueueAccess()
        {
            if (queue != NodeQueue.Current) throw new InvalidOperationException("Cannot access to node from this thread");
        }

        internal void Schedule()
        {
            if ((state & NodeState.Scheduled) == NodeState.Scheduled) return;
            state |= NodeState.Scheduled;
            queue.Schedule(this);
        }

        internal void ScheduledExecute()
        {
            state &= ~NodeState.Scheduled;
            OnScheduledExecute();
        }

        protected virtual void OnScheduledExecute() { }
    }
}
