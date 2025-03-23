using System;
using System.Collections;
using System.Collections.Generic;

namespace Lazy
{
    public class OrderedHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly HashSet<T> _hashSet = new();
        private readonly List<T> _list = new();

        public int Count => _hashSet.Count;
        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            if (_hashSet.Add(item))
            {
                _list.Add(item);
                return true;
            }

            return false;
        }

        public bool Remove(T item)
        {
            if (_hashSet.Remove(item))
            {
                _list.Remove(item);
                return true;
            }

            return false;
        }

        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public void Clear()
        {
            _hashSet.Clear();
            _list.Clear();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public T this[int index] => _list[index];

        /// <summary>
        /// 根据提供的 keySelector 对元素重新排序
        /// </summary>
        public void Sort<TKey>(Func<T, TKey> keySelector)
        {
            _list.Sort((a, b) => Comparer<TKey>.Default.Compare(keySelector(a), keySelector(b)));
        }
    }
}
