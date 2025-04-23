using System;
using System.Collections;
using System.Collections.Generic;

namespace Lazy
{
    public abstract class ABSTable<T> : IEnumerable<T>, IDisposable
    {
        public void Add(T item)
        {
            OnAdd(item);
        }

        public void Remove(T item)
        {
            OnRemove(item);
        }

        public void Clear()
        {
            OnClear();
        }

        protected abstract void OnAdd(T item);
        protected abstract void OnRemove(T item);

        protected abstract void OnClear();

        protected abstract void OnDispose();

        public void Dispose()
        {
            OnDispose();
        }

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
