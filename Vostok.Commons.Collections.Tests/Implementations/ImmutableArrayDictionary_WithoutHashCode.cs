using System;
using System.Collections.Generic;
using System.Threading;

namespace Vostok.Commons.Collections.Tests.Implementations
{
    internal class ImmutableArrayDictionary_WithoutHashCode<TKey, TValue>
    {
        private const int DefaultCapacity = 4;

        public static readonly ImmutableArrayDictionary<TKey, TValue> Empty = new ImmutableArrayDictionary<TKey, TValue>(0);

        private readonly Pair[] pairs;

        private readonly IEqualityComparer<TKey> keyComparer;

        public ImmutableArrayDictionary_WithoutHashCode(IEqualityComparer<TKey> keyComparer = null)
            : this(DefaultCapacity, keyComparer)
        {
        }

        public ImmutableArrayDictionary_WithoutHashCode(int capacity, IEqualityComparer<TKey> keyComparer = null)
            : this(new Pair[capacity], 0, keyComparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative");
        }

        private ImmutableArrayDictionary_WithoutHashCode(Pair[] pairs, int count, IEqualityComparer<TKey> keyComparer)
        {
            this.pairs = pairs;
            this.keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            Count = count;
        }

        public int Count { get; }

        public ImmutableArrayDictionary_WithoutHashCode<TKey, TValue> Set(TKey key, TValue value, bool overwrite = true)
        {
            Pair[] newProperties;

            var newProperty = new Pair(key, value);

            if (Find(key, out var oldValue, out var oldIndex))
            {
                if (!overwrite || Equals(value, oldValue))
                    return this;

                newProperties = ReallocateArray(pairs.Length);
                newProperties[oldIndex] = newProperty;
                return new ImmutableArrayDictionary_WithoutHashCode<TKey, TValue>(newProperties, Count, keyComparer);
            }

            if (pairs.Length == Count)
            {
                newProperties = ReallocateArray(Math.Max(DefaultCapacity, pairs.Length * 2));
                newProperties[Count] = newProperty;
                return new ImmutableArrayDictionary_WithoutHashCode<TKey, TValue>(newProperties, Count + 1, keyComparer);
            }

            if (Interlocked.CompareExchange(ref pairs[Count], newProperty, null) != null)
            {
                newProperties = ReallocateArray(pairs.Length);
                newProperties[Count] = newProperty;
                return new ImmutableArrayDictionary_WithoutHashCode<TKey, TValue>(newProperties, Count + 1, keyComparer);
            }

            return new ImmutableArrayDictionary_WithoutHashCode<TKey, TValue>(pairs, Count + 1, keyComparer);
        }
        
        public bool TryGetValue(TKey key, out TValue value) =>
            Find(key, out value, out _);

        private bool Find(TKey key, out TValue value, out int index)
        {
            for (var i = 0; i < Count; i++)
            {
                var property = pairs[i];
                if (keyComparer.Equals(property.Key, key))
                {
                    index = i;
                    value = property.Value;
                    return true;
                }
            }

            index = -1;
            value = default;
            return false;
        }

        private Pair[] ReallocateArray(int capacity)
        {
            var reallocated = new Pair[capacity];

            Array.Copy(pairs, 0, reallocated, 0, Count);

            return reallocated;
        }

        private class Pair
        {
            public Pair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public TKey Key { get; }
            public TValue Value { get; }
        }
    }
}