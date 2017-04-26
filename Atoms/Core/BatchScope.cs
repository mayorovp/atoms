using System;

namespace Atoms.Core
{
    public struct BatchScope : IDisposable
    {
        private NodeQueue queue;

        public BatchScope(NodeQueue queue)
        {
            this.queue = queue;

            queue.StartBatch();
        }

        public static BatchScope Create()
        {
            return new BatchScope(NodeQueue.Current);
        }

        public void Dispose()
        {
            queue.EndBatch();
        }
    }
}
