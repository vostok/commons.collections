using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    [PublicAPI]
#if MAKE_CLASSES_PUBLIC
    public
#else
    internal
#endif
    class RecyclingBoundedCache<TKey, TValue>
    {
        private readonly int capacity;
        private readonly IEqualityComparer<TKey> comparer;
        private RecyclingBoundedCacheState state;

        public RecyclingBoundedCache(int capacity, [CanBeNull] IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 0)
                throw new ArgumentException("The capacity must be non-negative");

            this.capacity = capacity;
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;

            state = new RecyclingBoundedCacheState(this.comparer);
        }

        public TValue Obtain(TKey key, Func<TKey, TValue> factory)
        {
            var currentState = state;
            if (currentState.Items.TryGetValue(key, out var value))
                return value;

            if (currentState.Items.TryAdd(key, value = factory(key)))
            {
                var newCount = Interlocked.Increment(ref currentState.Count);
                if (newCount > capacity)
                    Interlocked.Exchange(ref state, new RecyclingBoundedCacheState(comparer));
            }

            return value;
        }

        private class RecyclingBoundedCacheState
        {
            public readonly ConcurrentDictionary<TKey, TValue> Items;

            public int Count;

            public RecyclingBoundedCacheState(IEqualityComparer<TKey> comparer)
            {
                Items = new ConcurrentDictionary<TKey, TValue>(comparer);
            }
        }
    }
}