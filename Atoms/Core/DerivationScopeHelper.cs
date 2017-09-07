using System;

namespace Atoms.Core
{
    struct DerivationScopeHelper : IDisposable
    {
        private readonly DerivationScope oldScope, scope;

        public DerivationScopeHelper(IDerivation derivation, ref DerivationScope scope)
        {
            this.oldScope = scope;
            this.scope = scope = new DerivationScope(derivation);

            oldScope?.Stop();
        }

        public void Dispose()
        {
            oldScope?.Finish(scope);
        }

        public void Execute(Action action) => scope.Execute(action);
        public T Execute<T>(Func<T> action) => scope.Execute(action);
    }
}
