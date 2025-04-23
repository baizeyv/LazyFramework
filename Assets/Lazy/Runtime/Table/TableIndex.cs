using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Pool;

namespace Lazy
{
    public class TableIndex<TKey, TValue> : IDisposable
    {
        private Dictionary<TKey, List<TValue>> _index = DictionaryPool<TKey, List<TValue>>.Obtain();

        private Func<TValue, TKey> _getKeyByValue = null;

        public TableIndex(Func<TValue, TKey> getKeyByValue)
        {
            _getKeyByValue = getKeyByValue;
        }

        public IDictionary<TKey, List<TValue>> Dictionary => _index;

        public void Add(TValue value)
        {
            var key = _getKeyByValue(value);
            if (_index.ContainsKey(key))
            {
                _index[key].Add(value);
            }
            else
            {
                var list = ListPool<TValue>.Obtain();
                list.Add(value);
                _index.Add(key, list);
            }
        }

        public void Remove(TValue value)
        {
            var key = _getKeyByValue(value);
            _index[key].Remove(value);
        }

        public IEnumerable<TValue> Get(TKey key)
        {
            return _index.TryGetValue(key, out var retList) ? retList : Enumerable.Empty<TValue>();
        }

        public void Clear()
        {
            foreach (var value in _index.Values)
                value.Clear();
            _index.Clear();
        }

        public void Dispose()
        {
            foreach (var value in _index.Values)
                value.Free();
            _index.Free();
            _index = null;
        }
    }
}
