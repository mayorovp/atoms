using System;
using System.Collections.Generic;

namespace Atoms
{
    public partial class Atom
    {
        public static Computed<T> Computed<T>(Func<T> expr, IEqualityComparer<T> comparer = null)
            => new Computed<T>(expr, comparer ?? EqualityComparer<T>.Default);

        public static IDisposable Autorun(Action action)
            => new AutorunReaction(action);

        public static IDisposable When(Func<bool> condition, Action action)
            => new WhenReaction(condition, action);

        public static T Expr<T>(Func<T> expr, IEqualityComparer<T> comparer = null)
            => Computed(expr, comparer).Value;

        public static Core.BatchScope StartBatch() => Core.BatchScope.Create();
    }
}
