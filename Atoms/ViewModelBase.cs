using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Atoms
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private readonly Dictionary<string, Atom> atoms = new Dictionary<string, Atom>();

        protected T GetProperty<T>(T value, [CallerMemberName]string propName = null)
        {
            Atom atom;
            if (!atoms.TryGetValue(propName, out atom))
                atoms.Add(propName, atom = new Atom());

            atom.ReportObserved();
            return value;
        }

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName]string propName = null)
        {
            field = value;

            Atom atom;
            if (atoms.TryGetValue(propName, out atom))
                atom.ReportDirty();

            RaisePropertyChanged(propName);
        }

        protected T Computed<T>(Func<T> expr, IEqualityComparer<T> comparer = null, [CallerMemberName]string propName = null)
            => new ComputedProperty<T>(expr, EqualityComparer<T>.Default, this, propName).Value;

        protected void RaisePropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public event PropertyChangedEventHandler PropertyChanged;

        private class ComputedProperty<T> : Core.ComputedBase<T>
        {
            private readonly IEqualityComparer<T> equalityComparer;
            private readonly Func<T> expr;
            private readonly ViewModelBase owner;
            private readonly string propName;

            public ComputedProperty(Func<T> expr, IEqualityComparer<T> equalityComparer, ViewModelBase owner, string propName)
            {
                this.expr = expr;
                this.equalityComparer = equalityComparer;
                this.owner = owner;
                this.propName = propName;

                IsWatched = true;
            }

            protected override bool OnCompare(T oldValue, T newValue) => equalityComparer.Equals(oldValue, newValue);
            protected override T OnCompute() => expr();

            protected override void OnChanged()
            {
                IsWatched = false;
                owner.RaisePropertyChanged(propName);
            }
        }
    }
}
