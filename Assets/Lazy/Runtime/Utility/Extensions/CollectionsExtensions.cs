using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lazy.Utility
{
    public static class CollectionsExtensions
    {
        public static void Remove<T>(this Queue<T> self, T removeItem)
        {
            var count = self.Count;
            var removed = false;
            for (var i = 0; i < count; i++)
            {
                var item = self.Dequeue();
                if (!removed && item.Equals(removeItem))
                {
                    removed = true;
                    continue;
                }

                self.Enqueue(item);
            }
        }

        public static void Print<T>(this IEnumerable<T> self, Func<T, string> stringMethod = null)
        {
            if (self == null)
                return;

            var sb = new StringBuilder();
            sb.Append("[");
            var enumerable = self as T[] ?? self.ToArray();
            var cnt = enumerable.Count();
            for (var i = 0; i < cnt; i++)
            {
                if (stringMethod != null)
                    sb.Append(stringMethod.Invoke(enumerable[i]));
                else
                    sb.Append(enumerable[i]);
                if (i + 1 != cnt)
                    sb.Append(", ");
            }

            sb.Append("]");
            Log.Log.MsgD(sb.ToString());
        }
    }
}
