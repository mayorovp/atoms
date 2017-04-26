using System.Collections.Generic;

namespace Atoms.Core
{
    static class DerivationUtils
    {
        public static bool ShouldExecute(ref NodeState state, IEnumerable<AtomBase> dependencies)
        {
            if ((state & NodeState.Dirty) == NodeState.Dirty) return true;

            if ((state & NodeState.ProbablyDirty) == NodeState.ProbablyDirty)
            {
                foreach (var atom in dependencies)
                {
                    if (atom.DirtyCheck()) return true;
                }

                state &= ~NodeState.ProbablyDirty;
                foreach (var atom in dependencies)
                {
                    atom.SubscribeObserver(~NodeState.ProbablyDirty);
                }
            }

            return false;
        }
    }
}
