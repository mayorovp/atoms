using System;
using System.Collections.Generic;

namespace Atoms.Core
{
    struct DerivationScopeHelper : IDisposable
    {
        private readonly IDerivation derivation;
        private readonly HashSet<AtomBase> old_deps, new_deps;

        public DerivationScopeHelper(IDerivation derivation, ref HashSet<AtomBase> dependencies)
        {
            this.old_deps = dependencies;
            this.new_deps = dependencies = new HashSet<AtomBase>();
            this.derivation = derivation;
        }

        public void Dispose()
        {
            if (old_deps != null)
            {
                old_deps.ExceptWith(new_deps);
                foreach (var atom in old_deps)
                    atom.RemoveObserver(derivation);
            }
        }

        public void Execute(Action action) => new DerivationScope(derivation, new_deps).Execute(action);
        public T Execute<T>(Func<T> action) => new DerivationScope(derivation, new_deps).Execute(action);
    }
}
