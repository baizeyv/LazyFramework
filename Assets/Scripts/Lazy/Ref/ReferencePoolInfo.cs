using System;

namespace Lazy.Ref
{
    /// <summary>
    /// * 引用池信息
    /// </summary>
    public struct ReferencePoolInfo
    {
        private readonly Type _type;

        private readonly int _unusedReferenceCount;

        private readonly int _usingReferenceCount;

        private readonly int _obtainReferenceCount;

        private readonly int _freeReferenceCount;

        private readonly int _addReferenceCount;

        private readonly int _removeReferenceCount;

        public ReferencePoolInfo(Type type, int unusedReferenceCount, int usingReferenceCount, int obtainReferenceCount,
            int freeReferenceCount, int addReferenceCount, int removeReferenceCount)
        {
            _type = type;
            _unusedReferenceCount = unusedReferenceCount;
            _usingReferenceCount = usingReferenceCount;
            _obtainReferenceCount = obtainReferenceCount;
            _freeReferenceCount = freeReferenceCount;
            _addReferenceCount = addReferenceCount;
            _removeReferenceCount = removeReferenceCount;
        }

        /// <summary>
        /// 获取引用池类型。
        /// </summary>
        public Type Type => _type;

        /// <summary>
        /// 获取未使用引用数量。
        /// </summary>
        public int UnusedReferenceCount => _unusedReferenceCount;

        /// <summary>
        /// 获取正在使用引用数量。
        /// </summary>
        public int UsingReferenceCount => _usingReferenceCount;

        /// <summary>
        /// 获取获取引用数量。
        /// </summary>
        public int ObtainReferenceCount => _obtainReferenceCount;

        /// <summary>
        /// 获取归还引用数量。
        /// </summary>
        public int FreeReferenceCount => _freeReferenceCount;

        /// <summary>
        /// 获取增加引用数量。
        /// </summary>
        public int AddReferenceCount => _addReferenceCount;

        /// <summary>
        /// 获取移除引用数量。
        /// </summary>
        public int RemoveReferenceCount => _removeReferenceCount;
    }
}