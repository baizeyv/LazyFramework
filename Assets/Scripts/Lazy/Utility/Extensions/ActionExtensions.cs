using System;

namespace Lazy.Utility
{
    public static class ActionExtensions
    {
        public static void Fire(this Action self)
        {
            self?.Invoke();
        }

        public static void Fire<T>(this Action<T> self, T arg)
        {
            self?.Invoke(arg);
        }

        public static void Fire<T, TU>(this Action<T, TU> self, T arg, TU arg2)
        {
            self?.Invoke(arg, arg2);
        }

        public static void Fire<T, TU, TV>(this Action<T, TU, TV> self, T arg, TU arg2, TV arg3)
        {
            self?.Invoke(arg, arg2, arg3);
        }

        public static T Fire<T>(this Func<T> func)
        {
            return func != null ? func() ?? default(T) : default(T);
        }

        public static TU Fire<T, TU>(this Func<T, TU> func, T arg)
        {
            return func != null ? func(arg) ?? default(TU) : default(TU);
        }
    }
}