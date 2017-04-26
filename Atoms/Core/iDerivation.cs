namespace Atoms.Core
{
    internal interface IDerivation
    {
        void ReportStateChanged(NodeState state);
        void ReportObserved(AtomBase atom, object tag);
    }
}