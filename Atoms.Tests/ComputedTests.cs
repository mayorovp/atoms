using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Atoms.Tests
{
    [TestClass]
    public class ComputedTests
    {
        private readonly List<int> values = new List<int>();

        [TestInitialize]
        public void Initialize() => values.Clear();

        [TestCleanup]
        public void Cleanup() => Core.NodeQueue.Clear();

        [TestMethod]
        public void TestSingleAtomAndReaction()
        {
            var atom = new AtomRef<int>();
            Atom.Autorun(() => values.Add(atom.Value));

            atom.Value = 1;
            atom.Value = 2;
            atom.Value = 3;

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, values);
        }

        [TestMethod]
        public void TestComputedAtomAndReaction()
        {
            var a = new AtomRef<int>();
            var b = new AtomRef<int>();
            var c = Atom.Computed(() => a.Value + b.Value);
            Atom.Autorun(() => values.Add(c.Value));

            a.Value = 1;
            b.Value = 2;
            a.Value = 3;
            a.Value = 3;

            CollectionAssert.AreEqual(new[] { 0, 1, 3, 5 }, values);
        }

        [TestMethod]
        public void TestDeepTreeDepsAndReaction()
        {
            var a = new AtomRef<int> { Value = 1 };
            var b = new AtomRef<int> { Value = 0 };
            var c = Atom.Computed(() => a.Value + b.Value);
            var d = Atom.Computed(() => a.Value - b.Value);
            var e = Atom.Computed(() => (c.Value + d.Value) / a.Value);
            var f = Atom.Computed(() => e.Value + b.Value);
            Atom.Autorun(() => values.Add(f.Value));

            a.Value = 1;
            b.Value = 2;
            a.Value = 3;
            b.Value = 4;
            a.Value = 5;
            b.Value = 6;

            CollectionAssert.AreEqual(new[] { 2, 4, 6, 8 }, values);
        }

        [TestMethod]
        public void TestBatchAndReaction()
        {
            var a = new AtomRef<int> { Value = 1 };
            var b = new AtomRef<int> { Value = 0 };
            var c = Atom.Computed(() => a.Value + b.Value);
            var d = Atom.Computed(() => a.Value - b.Value);
            var e = Atom.Computed(() => (c.Value + d.Value) / a.Value);
            var f = Atom.Computed(() => e.Value + b.Value);
            Atom.Autorun(() => values.Add(f.Value));

            using (Atom.StartBatch())
            {
                a.Value = 1;
                b.Value = 2;
                a.Value = 3;
                b.Value = 4;
                a.Value = 5;
                b.Value = 6;
            }

            CollectionAssert.AreEqual(new[] { 2, 8 }, values);
        }

        [TestMethod]
        public void TestStopAutorun()
        {
            var atom = new AtomRef<int>();
            var run = Atom.Autorun(() => values.Add(atom.Value));

            atom.Value = 1;
            atom.Value = 2;
            run.Dispose();
            atom.Value = 3;

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, values);
        }

        [TestMethod]
        public void TestRepeatAutorunOnInternalChanges()
        {
            var atom = new AtomRef<int>();
            Atom.Autorun(() => {
                values.Add(atom.Value);
                atom.Value = 1;
            });

            CollectionAssert.AreEqual(new[] { 0, 1 }, values);
        }

        [TestMethod]
        public void TestNoStackOverflow()
        {
            var atom = new AtomRef<int>();
            Atom.Autorun(() => {
                if (atom.Value < 1000000)
                    atom.Value++;
            });
            Assert.AreEqual(1000000, atom.Value);
        }

        [TestMethod]
        public void TestWhen()
        {
            var atom = new AtomRef<int>();
            Atom.When(() => atom.Value > 5, () => values.Add(atom.Value));

            for (var i = 0; i < 10; i++)
                atom.Value = i;

            CollectionAssert.AreEqual(new[] { 6 }, values);
        }

        [TestMethod]
        public void TestWithoutExpr()
        {
            var a = new AtomRef<int>();
            var b = new AtomRef<int>();

            Atom.Autorun(() => {
                values.Add(a.Value + b.Value);
            });

            a.Value = 1;
            b.Value = 2;

            using (Atom.StartBatch())
            {
                a.Value = 2;
                b.Value = 1;
            }

            b.Value = 3;

            CollectionAssert.AreEqual(new[] { 0, 1, 3, 3, 5 }, values);
        }

        [TestMethod]
        public void TestWithExpr()
        {
            var a = new AtomRef<int>();
            var b = new AtomRef<int>();

            Atom.Autorun(() => {
                values.Add(Atom.Expr(() => a.Value + b.Value));
            });

            a.Value = 1;
            b.Value = 2;

            using (Atom.StartBatch())
            {
                a.Value = 2;
                b.Value = 1;
            }

            b.Value = 3;

            CollectionAssert.AreEqual(new[] { 0, 1, 3, 5 }, values);
        }
    }
}
