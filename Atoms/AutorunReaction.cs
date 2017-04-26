using System;

namespace Atoms
{
    internal class AutorunReaction : Core.ReactionBase
    {
        private Action action;

        public AutorunReaction(Action action)
        {
            this.action = action;

            Schedule();
        }

        protected override void Invalidate() => Track(() =>
        {
            using (Core.BatchScope.Create()) action();
        });
    }
}