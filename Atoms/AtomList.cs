using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Atoms
{
    public class AtomList<T> : IList<T>, INotifyCollectionChanged, IReadWriteAtom<IEnumerable<T>>
    {
        private readonly NotifyCollectionChangedStub localSource = new NotifyCollectionChangedStub();
        private readonly Atom valueAtom = new Atom(), countAtom = new Atom();
        private readonly List<Atom> atoms = new List<Atom>();
        private INotifyCollectionChanged source;
        private IList<T> inner;

        public AtomList() : this(new List<T>()) { }

        public AtomList(IEnumerable<T> inner)
        {
            source = localSource;
            this.Value = inner;
        }

        public IEnumerable<T> Value
        {
            get
            {
                valueAtom.ReportObserved();
                return inner.AsEnumerable();
            }
            set
            {
                if (inner == value || inner == source) return;

                using (Atom.StartBatch())
                {
                    source.CollectionChanged -= Source_CollectionChanged;

                    inner = value as IList<T> ?? value.ToList();
                    source = value as INotifyCollectionChanged ?? localSource;

                    source.CollectionChanged += Source_CollectionChanged;

                    valueAtom.ReportDirty();
                    if (atoms.Count != inner.Count) countAtom.ReportDirty();

                    TouchAtoms(0, atoms.Count);
                    if (atoms.Count < inner.Count) InsertAtoms(atoms.Count, inner.Count - atoms.Count);
                    if (atoms.Count > inner.Count) RemoveAtoms(inner.Count, atoms.Count - inner.Count);
                }
            }
        }

        public IEnumerable<T> Peek() => inner.AsEnumerable();

        private void TouchAtoms(int start, int end)
        {
            for (var i = start; i < end; i++) atoms[i].ReportDirty();
        }
        private void RemoveAtoms(int pos, int count) => atoms.RemoveRange(pos, count);
        private void InsertAtoms(int pos, int count) => atoms.InsertRange(pos, Enumerable.Range(pos, count).Select(_ => new Atom()));

        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            valueAtom.ReportDirty();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (atoms.Count > 0) countAtom.ReportDirty();
                    TouchAtoms(0, atoms.Count);
                    RemoveAtoms(0, atoms.Count);
                    break;

                case NotifyCollectionChangedAction.Add:
                    countAtom.ReportDirty();
                    TouchAtoms(e.NewStartingIndex, atoms.Count);
                    InsertAtoms(atoms.Count, e.NewItems.Count);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    countAtom.ReportDirty();
                    TouchAtoms(e.OldStartingIndex, atoms.Count);
                    RemoveAtoms(atoms.Count - e.OldItems.Count, e.OldItems.Count);
                    break;

                case NotifyCollectionChangedAction.Move:
                    TouchAtoms(Math.Min(e.OldStartingIndex, e.NewStartingIndex), Math.Max(e.OldStartingIndex + e.OldItems.Count, e.NewStartingIndex + e.NewItems.Count));
                    break;

                case NotifyCollectionChangedAction.Replace:
                    var diff = e.NewItems.Count - e.OldItems.Count;
                    if (diff == 0)
                        TouchAtoms(e.OldStartingIndex, e.OldStartingIndex + e.OldItems.Count);
                    else
                    {
                        countAtom.ReportDirty();
                        TouchAtoms(e.OldStartingIndex, atoms.Count);

                        if (diff > 0)
                            InsertAtoms(atoms.Count, diff);
                        else
                            RemoveAtoms(atoms.Count + diff, -diff);
                    }
                    break;
            }

            CollectionChanged?.Invoke(this, e);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count
        {
            get
            {
                countAtom.ReportObserved();
                return inner.Count;
            }
        }

        public bool IsReadOnly => inner.IsReadOnly;

        public T this[int index]
        {
            get
            {
                atoms[index].ReportObserved();
                return inner[index];
            }

            set
            {
                using (Atom.StartBatch())
                {
                    var oldValue = inner[index];
                    inner[index] = value;
                    localSource.Replace(index, oldValue, value);
                }
            }
        }

        public int IndexOf(T item)
        {
            valueAtom.ReportObserved();
            return inner.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            using (Atom.StartBatch())
            {
                inner.Insert(index, item);
                localSource.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            using (Atom.StartBatch())
            {
                var oldItem = inner[index];
                inner.RemoveAt(index);
                localSource.Remove(index, oldItem);
            }
        }

        public void Add(T item)
        {
            using (Atom.StartBatch())
            {
                inner.Add(item);
                localSource.Insert(inner.Count - 1, item);
            }
        }

        public void Clear()
        {
            using (Atom.StartBatch())
            {
                inner.Clear();
                localSource.Reset();
            }
        }

        public bool Contains(T item)
        {
            valueAtom.ReportObserved();
            return inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            valueAtom.ReportObserved();
            inner.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var index = inner.IndexOf(item);
            if (index != -1)
            {
                using (Atom.StartBatch())
                {
                    inner.RemoveAt(index);
                    localSource.Remove(index, item);
                }
                return true;
            }
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            valueAtom.ReportObserved();
            foreach (var item in inner) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class NotifyCollectionChangedStub : INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public void Replace(int index, T oldValue, T value) 
                => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldValue, index));

            internal void Insert(int index, T item)
                => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));

            internal void Remove(int index, T oldItem)
                => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));

            internal void Reset()
                => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
