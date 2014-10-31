using System;
using System.Threading;

#pragma warning disable 420

namespace Pavel.Atoms
{
    class StateGuard : IDisposable
    {
        private static readonly object INITIAL = "INITIAL";
        private static readonly object WAITING = "WAITING";
        private static readonly object DONE = "DONE";
        private volatile object state = INITIAL;

        public void Wait()
        {
            if (state == DONE || Interlocked.CompareExchange(ref state, WAITING, INITIAL) == DONE) return;
            lock (this)
            {
                if (state == DONE) return;
                Monitor.Wait(this);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref state, DONE) == WAITING)
                lock (this) Monitor.PulseAll(this);
        }
    }
}
