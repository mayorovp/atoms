using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Atoms.Tests
{
    [TestClass]
    public class ListTests
    {
        private readonly List<int> values = new List<int>();

        [TestInitialize]
        public void Initialize() => values.Clear();

        [TestCleanup]
        public void Cleanup() => Core.NodeQueue.Clear();

        [TestMethod]
        public void TestAtomListSimple()
        {
            var list = new AtomList<int> { 1, 2, 3 };

            Atom.Autorun(() => values.Add(list[1]));

            list.Add(4);
            list.RemoveAt(0);
            list.Insert(0, 5);
            list.Insert(1, 6);
            list[1] = 7;

            CollectionAssert.AreEqual(new[] { 2, 3, 2, 6, 7}, values);
        }
    }
}
