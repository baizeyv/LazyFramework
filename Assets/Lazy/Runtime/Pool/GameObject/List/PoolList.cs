using System;
using System.Runtime.CompilerServices;

namespace Lazy.Pool.GameObject
{
    internal sealed class PoolList<T>
    {
        internal T[] Components;
        internal int Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PoolList(int capacity = 32)
        {
#if DEBUG
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity must be greater than zero!"
                );
#endif
            Components = new T[capacity];
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(in T component)
        {
            if (Count >= Components.Length)
                Array.Resize(ref Components, Components.Length << 1);

            Components[Count++] = component;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveUnorderedAt(int id)
        {
            var lastComponentId = Count - 1;
            Components[id] = Components[lastComponentId];
            Components[lastComponentId] = default;
            Count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveAt(int id)
        {
#if DEBUG
            CheckForRemove(id);
#endif
            for (var i = id; i < Count; i++)
                Components[i] = i + 1 < Count ? Components[i + 1] : default;

            Count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            Array.Clear(Components, 0, Count);
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetCapacity(int capacity)
        {
#if DEBUG
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity must be greater than zero!"
                );
#endif
            if (Components.Length == capacity)
                return;

            Array.Resize(ref Components, capacity);

            if (Count > capacity)
                Count = capacity;
        }

#if DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckForRemove(int id)
        {
            if (Count <= id)
                throw new ArgumentOutOfRangeException(nameof(id), "Index is greater than count!");

            if (Count <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "List is empty, nothing to remove!"
                );
        }
#endif
    }
}
