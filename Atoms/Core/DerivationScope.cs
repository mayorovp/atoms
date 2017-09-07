using System;
using System.Collections.Generic;

namespace Atoms.Core
{
    internal class DerivationScope : ContextScope
    {
        private readonly NodeQueue queue = NodeQueue.Current;
        private readonly HashSet<AtomBase> dependencies = new HashSet<AtomBase>();
        private readonly IDerivation derivation;
        private bool alive = true;

        public DerivationScope(IDerivation derivation)
        {
            this.derivation = derivation;
        }

        public void Stop()
        {
            alive = false;
        }

        public void Finish(DerivationScope newScope)
        {
            if (newScope != null)
                dependencies.ExceptWith(newScope.dependencies);

            foreach (var atom in dependencies)
                atom.RemoveObserver(derivation);
        }

        public void ReportObserved(AtomBase atom)
        {
            if (!alive) return;

            if (queue != NodeQueue.Current) throw new InvalidOperationException("Cannot access to node from this thread");

            if (dependencies.Add(atom))
            {
                atom.AddObserver(derivation);
            }
        }

        public bool ShouldExecute(ref NodeState state)
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
