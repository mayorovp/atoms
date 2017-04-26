using System;

namespace Atoms
{
    internal class WhenReaction : Core.ReactionBase
    {
        private readonly Action action;
        private readonly Func<bool> condition;

        public WhenReaction(Func<bool> condition, Action action)
        {
            this.condition = condition;
            this.action = action;

            Schedule();
        }

        protected override void Invalidate()
        {
            if (Track(condition))
            {
                Dispose();
                using (Core.BatchScope.Create()) action();
            }
        }
    }
}