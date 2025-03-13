using System.Collections.Generic;

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
    }
}
