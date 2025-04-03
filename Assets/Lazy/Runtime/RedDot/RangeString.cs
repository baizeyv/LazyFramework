using System;

namespace Lazy.RedDot
{
    public class RangeString : IEquatable<RangeString>
    {
        /// <summary>
        /// * 源字符串
        /// </summary>
        private string _source;

        /// <summary>
        /// * 起始索引
        /// </summary>
        private int _startIndex;

        /// <summary>
        /// * 结束索引
        /// </summary>
        private int _endIndex;

        /// <summary>
        /// * 长度
        /// </summary>
        private int _length;

        /// <summary>
        /// * 源字符串是否为空
        /// </summary>
        private bool _isSourceNullOrEmpty;

        /// <summary>
        /// * Hash
        /// </summary>
        private int _hashCode;

        public RangeString(string source, int startIndex, int endIndex)
        {
            _source = source;
            _startIndex = startIndex;
            _endIndex = endIndex;
            _length = endIndex - startIndex + 1;
            _isSourceNullOrEmpty = string.IsNullOrEmpty(source);
            _hashCode = 0;
        }

        public bool Equals(RangeString other)
        {
            var isOtherNullOrEmpty = string.IsNullOrEmpty(other._source);

            if (_isSourceNullOrEmpty && isOtherNullOrEmpty)
                return true;

            if (_isSourceNullOrEmpty || isOtherNullOrEmpty)
                return false;

            if (_length != other._length)
                return false;

            for (int i = _startIndex, j = other._startIndex; i < _endIndex; i++, j++)
                if (_source[i] != other._source[j])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0 && !_isSourceNullOrEmpty)
                for (var i = _startIndex; i <= _endIndex; i++)
                    _hashCode = 31 * _hashCode + _source[i];

            return _hashCode;
        }

        public override string ToString()
        {
            // TODO:
            return base.ToString();
        }
    }
}
