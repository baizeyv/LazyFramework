using System;
using System.Collections.Generic;

namespace Lazy.Ref
{
    internal sealed class ReferenceCollection
    {
        /// <summary>
        /// * 引用队列
        /// </summary>
        private readonly Queue<IReference> _referencesQueue;

        /// <summary>
        /// * 引用类型
        /// </summary>
        private readonly Type _referenceType;

        /// <summary>
        /// * 正在使用的引用数量
        /// </summary>
        private int _usingReferenceCount;

        /// <summary>
        /// * 获取的引用数量
        /// </summary>
        private int _obtainReferenceCount;

        /// <summary>
        /// * 释放的引用数量
        /// </summary>
        private int _freeReferenceCount;

        /// <summary>
        /// * 添加的引用数量
        /// </summary>
        private int _addReferenceCount;

        /// <summary>
        /// * 移除的引用数量
        /// </summary>
        private int _removeReferenceCount;

        /// <summary>
        /// * Constructor
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        public ReferenceCollection(Type referenceType)
        {
            _referencesQueue = new();
            _referenceType = referenceType;
            _usingReferenceCount = 0;
            _obtainReferenceCount = 0;
            _freeReferenceCount = 0;
            _addReferenceCount = 0;
            _removeReferenceCount = 0;
        }

        public Type ReferenceType => _referenceType;

        /// <summary>
        /// * 未使用的引用类型
        /// </summary>
        public int UnusedReferenceCount => _referencesQueue.Count;

        public int UsingReferenceCount => _usingReferenceCount;

        public int ObtainReferenceCount => _obtainReferenceCount;

        public int FreeReferenceCount => _freeReferenceCount;

        public int AddReferenceCount => _addReferenceCount;

        public int RemoveReferenceCount => _removeReferenceCount;

        public T Obtain<T>() where T : class, IReference, new()
        {
            if (typeof(T) != _referenceType)
            {
                Log.Log.MsgE("Type is invalid.");
                return null;
            }

            _usingReferenceCount++;
            _obtainReferenceCount++;

            lock (_referencesQueue)
            {
                if (_referencesQueue.Count > 0)
                {
                    return _referencesQueue.Dequeue() as T;
                }
            }

            _addReferenceCount++;
            return new T();
        }

        public IReference Obtain()
        {
            _usingReferenceCount++;
            _obtainReferenceCount++;
            lock (_referencesQueue)
            {
                if (_referencesQueue.Count > 0)
                {
                    return _referencesQueue.Dequeue();
                }
            }

            _addReferenceCount++;
            return Activator.CreateInstance(_referenceType) as IReference;
        }

        public void Free(IReference reference)
        {
            reference.Clear();
            lock (_referencesQueue)
            {
                if (_referencesQueue.Contains(reference))
                {
                    Log.Log.MsgE("Reference is already freed.");
                    return;
                }
                _referencesQueue.Enqueue(reference);
            }

            _freeReferenceCount++;
            _usingReferenceCount--;
        }

        public void Add<T>(int count) where T : class, IReference, new()
        {
            if (typeof(T) != _referenceType)
            {
                Log.Log.MsgE("Type is invalid.");
                return ;
            }

            lock (_referencesQueue)
            {
                _addReferenceCount += count;
                while (count-- > 0)
                {
                    _referencesQueue.Enqueue(new T());
                }
            }
        }

        public void Add(int count)
        {
            lock (_referencesQueue)
            {
                _addReferenceCount += count;
                while (count -- > 0)
                {
                    _referencesQueue.Enqueue(Activator.CreateInstance(_referenceType) as IReference);
                }
            }
        }

        public void Remove(int count)
        {
            lock (_referencesQueue)
            {
                if (count > _referencesQueue.Count)
                {
                    count = _referencesQueue.Count;
                }

                _removeReferenceCount += count;

                while (count -- > 0)
                {
                    _referencesQueue.Dequeue();
                }
            }
        }

        public void RemoveAll()
        {
            lock (_referencesQueue)
            {
                _removeReferenceCount += _referencesQueue.Count;
                _referencesQueue.Clear();
            }
        }
    }
}