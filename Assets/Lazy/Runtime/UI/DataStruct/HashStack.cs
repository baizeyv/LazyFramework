using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lazy.UI
{
    public class HashStack<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _hashSet = new();
        private readonly List<T> _list = new(); // 用 List<T> 来支持排序

        public int Count => _list.Count;

        public bool Push(T item)
        {
            if (_hashSet.Add(item))
            {
                _list.Add(item);
                return true;
            }

            return false;
        }

        public bool TryPop(out T item)
        {
            if (_list.Count > 0)
            {
                item = _list[^1]; // 获取最后一个元素
                _list.RemoveAt(_list.Count - 1);
                _hashSet.Remove(item);
                return true;
            }

            item = default!;
            return false;
        }

        public T Peek()
        {
            return _list[^1];
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
            return ((IEnumerable<T>)_list).Reverse().GetEnumerator();
            // 逆序迭代，符合栈的特性
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
