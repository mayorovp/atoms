namespace Atoms.Core
{
    public interface IDerivation
    {
        void ReportDependencyStateChanged(NodeState state);
        NodeState DependencyStateMask { get; }
    }
}