using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Table;

namespace Lazy
{
    public class PanelTable : ABSTable<IPanel>
    {
        private TableIndex<string, IPanel> _gameObjectNameIndex = new(x => x.Transform.name);

        private TableIndex<Type, IPanel> _typeIndex = new(x => x.GetType());

        public IEnumerable<IPanel> Search(PanelKey key)
        {
            if (
                key.PanelType != null
                && (!string.IsNullOrEmpty(key.GameObjectName) || key.Panel != null)
            )
                return _typeIndex
                    .Get(key.PanelType)
                    .Where(x => x.Transform.name == key.GameObjectName || x == key.Panel);

            if (key.PanelType != null)
                return _typeIndex.Get(key.PanelType);

            if (key.Panel != null)
                return _gameObjectNameIndex.Get(key.Panel.Transform.gameObject.name);

            if (!string.IsNullOrEmpty(key.GameObjectName))
                return _gameObjectNameIndex.Get(key.GameObjectName);
            return Enumerable.Empty<IPanel>();
        }

        protected override void OnAdd(IPanel item)
        {
            _gameObjectNameIndex.Add(item);
            _typeIndex.Add(item);
        }

        protected override void OnRemove(IPanel item)
        {
            _gameObjectNameIndex.Remove(item);
            _typeIndex.Remove(item);
        }

        protected override void OnClear()
        {
            _gameObjectNameIndex.Clear();
            _typeIndex.Clear();
        }

        protected override void OnDispose()
        {
            _gameObjectNameIndex.Dispose();
            _typeIndex.Dispose();
            _gameObjectNameIndex = null;
            _typeIndex = null;
        }

        public override IEnumerator<IPanel> GetEnumerator()
        {
            return _gameObjectNameIndex.Dictionary.SelectMany(x => x.Value).GetEnumerator();
        }
    }
}
