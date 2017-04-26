using System;
using System.Threading;

namespace Atoms.Core
{
    internal class ContextScope : SynchronizationContext
    {
        private readonly SynchronizationContext outer = Current;

        public override void Send(SendOrPostCallback d, object state) => outer.Send(ExecuteWorkItem, new WorkItem(d, state));
        public override void Post(SendOrPostCallback d, object state) => outer.Post(ExecuteWorkItem, new WorkItem(d, state));

        private void ExecuteWorkItem(object state)
        {
            var item = (WorkItem)state;
            using (new SyncCtxSwitch(this))
                item.Execute();
        }

        public void Execute(Action action)
        {
            using (new SyncCtxSwitch(this))
                action();
        }

        public T Execute<T>(Func<T> action)
        {
            using (new SyncCtxSwitch(this))
                return action();
        }

        private struct SyncCtxSwitch : IDisposable
        {
            private readonly SynchronizationContext old;

            public SyncCtxSwitch(SynchronizationContext ctx)
            {
                this.old = Current;
                SetSynchronizationContext(ctx);
            }

            public void Dispose()
            {
                SetSynchronizationContext(old);
            }
        }

        private class WorkItem
        {
            private SendOrPostCallback d;
            private object state;

            public WorkItem(SendOrPostCallback d, object state)
            {
                this.d = d;
                this.state = state;
            }

            public void Execute() => d(state);
        }
    }
}
