using System;
using System.Collections.Generic;

namespace Lazy.Rx
{
    public sealed class ClampedReactiveVariable<T> : ReactiveVariable<T>
        where T : IComparable<T>
    {
        private readonly T _min,
            _max;

        private static IComparer<T> Comparer { get; } = Comparer<T>.Default;

        public ClampedReactiveVariable(T initialValue, T min, T max)
            : base(initialValue)
        {
            _min = min;
            _max = max;
        }

        protected override void OnValueChanging(ref T value)
        {
            if (Comparer.Compare(value, _min) < 0)
                value = _min;
            else if (Comparer.Compare(value, _max) > 0)
                value = _max;
        }
    }
}
