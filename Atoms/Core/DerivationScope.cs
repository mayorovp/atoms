namespace Atoms.Core
{
    internal class DerivationScope : ContextScope
    {
        private IDerivation derivation;
        private object tag;

        public DerivationScope(IDerivation derivation, object tag)
        {
            this.derivation = derivation;
            this.tag = tag;
        }

        internal void ReportObserved(AtomBase atom) => derivation.ReportObserved(atom, tag);
    }
}
