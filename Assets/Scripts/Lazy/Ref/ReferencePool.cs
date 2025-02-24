using System;
using System.Collections.Generic;
using Lazy.Singleton;

namespace Lazy.Ref
{
    public class ReferencePool : Singleton<ReferencePool>
    {
        private readonly Dictionary<Type, ReferenceCollection> _referenceCollections = new();

        /// <summary>
        /// * 获取引用池数量
        /// </summary>
        public int Count => _referenceCollections.Count;

        /// <summary>
        /// 获取所有引用池的信息。
        /// </summary>
        /// <returns>所有引用池的信息。</returns>
        public ReferencePoolInfo[] GetAllReferencePoolInfos()
        {
            int index = 0;
            ReferencePoolInfo[] results = null;

            lock (_referenceCollections)
            {
                results = new ReferencePoolInfo[_referenceCollections.Count];
                foreach (var item in _referenceCollections)
                {
                    results[index++] = new ReferencePoolInfo(item.Key, item.Value.UnusedReferenceCount,
                        item.Value.UsingReferenceCount, item.Value.ObtainReferenceCount, item.Value.FreeReferenceCount,
                        item.Value.AddReferenceCount, item.Value.RemoveReferenceCount
                    );
                }
            }

            return results;
        }

        /// <summary>
        /// 清除所有引用池。
        /// </summary>
        public void ClearAll()
        {
            lock (_referenceCollections)
            {
                foreach (var item in _referenceCollections)
                {
                    item.Value.RemoveAll();
                }

                _referenceCollections.Clear();
            }
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <returns>引用。</returns>
        public T Obtain<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Obtain<T>();
        }

        /// <summary>
        /// 从引用池获取引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        /// <returns>引用。</returns>
        public IReference Obtain(Type referenceType)
        {
            InternalCheckReferenceType(referenceType);
            return GetReferenceCollection(referenceType).Obtain();
        }

        public void Free(IReference reference)
        {
            if (reference == null)
            {
                Log.Log.MsgE("Reference is invalid.");
                return;
            }

            var referenceType = reference.GetType();
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).Free(reference);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">追加数量。</param>
        public void Add<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        /// <param name="count">追加数量。</param>
        public void Add(Type referenceType, int count)
        {
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).Add(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        /// <param name="count">移除数量。</param>
        public void Remove<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        /// <param name="count">移除数量。</param>
        public void Remove(Type referenceType, int count)
        {
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除所有的引用。
        /// </summary>
        /// <typeparam name="T">引用类型。</typeparam>
        public void RemoveAll<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 从引用池中移除所有的引用。
        /// </summary>
        /// <param name="referenceType">引用类型。</param>
        public void RemoveAll(Type referenceType)
        {
            InternalCheckReferenceType(referenceType);
            GetReferenceCollection(referenceType).RemoveAll();
        }

        private void InternalCheckReferenceType(Type referenceType)
        {
            if (referenceType == null)
            {
                Log.Log.MsgE("Reference type is invalid.");
                return;
            }

            if (!referenceType.IsClass || referenceType.IsAbstract)
            {
                Log.Log.MsgE("Reference type is not a non-abstract class type.");
                return;
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                Log.Log.MsgE($"Reference type '{referenceType.FullName}' is invalid.");
            }
        }

        private ReferenceCollection GetReferenceCollection(Type referenceType)
        {
            if (referenceType == null)
            {
                Log.Log.MsgE("ReferenceType is invalid.");
                return null;
            }

            ReferenceCollection referenceCollection = null;
            lock (_referenceCollections)
            {
                if (_referenceCollections.TryGetValue(referenceType, out referenceCollection))
                    return referenceCollection;
                referenceCollection = new ReferenceCollection(referenceType);
                _referenceCollections.Add(referenceType, referenceCollection);
            }

            return referenceCollection;
        }
    }
}