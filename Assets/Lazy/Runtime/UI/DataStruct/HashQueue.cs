using System;
using System.Collections;
using System.Collections.Generic;

namespace Lazy
{
    public class HashQueue<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _hashSet = new();
        private readonly List<T> _list = new(); // 用 List<T> 来支持排序

        public int Count => _list.Count;

        public bool Enqueue(T item)
        {
            if (_hashSet.Add(item))
            {
                _list.Add(item);
                return true;
            }

            return false;
        }

        public bool TryDequeue(out T item)
        {
            if (_list.Count > 0)
            {
                item = _list[0]; // 获取队列头部元素
                _list.RemoveAt(0);
                _hashSet.Remove(item);
                return true;
            }

            item = default!;
            return false;
        }

        public T Peek()
        {
            return _list[0];
        }

        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        public void Clear()
        {
            _list.Clear();
            _hashSet.Clear();
        }

        public void Sort<TKey>(Func<T, TKey> keySelector)
        {
            _list.Sort((a, b) => Comparer<TKey>.Default.Compare(keySelector(a), keySelector(b)));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
