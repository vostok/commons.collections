using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    class ImmutableArrayDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private const int DefaultCapacity = 4;

        public static readonly ImmutableArrayDictionary<TKey, TValue> Empty = new ImmutableArrayDictionary<TKey, TValue>(0);

        private readonly Pair[] pairs;

        private readonly IEqualityComparer<TKey> keyComparer;

        public ImmutableArrayDictionary([CanBeNull] IEqualityComparer<TKey> keyComparer = null)
            : this(DefaultCapacity, keyComparer)
        {
        }

        public ImmutableArrayDictionary(int capacity, [CanBeNull] IEqualityComparer<TKey> keyComparer = null)
            : this(new Pair[capacity], 0, keyComparer)
        {
        }

        private ImmutableArrayDictionary(Pair[] pairs, int count, IEqualityComparer<TKey> keyComparer)
        {
            this.pairs = pairs;
            this.keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            Count = count;
        }

        public int Count { get; }

        public IEnumerable<TKey> Keys => this.Select(pair => pair.Key);

        public IEnumerable<TValue> Values => this.Select(pair => pair.Value);

        public TValue this[TKey key] => Find(key, out var value, out _) ? value : throw new KeyNotFoundException($"A value with key '{key}' is not present.");

        public bool ContainsKey(TKey key)
        {
            return Find(key, out _, out _);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Find(key, out value, out _);
        }

        public ImmutableArrayDictionary<TKey, TValue> Set(TKey key, TValue value, bool overwrite = true)
        {
            Pair[] newProperties;

            var newProperty = new Pair(key, value);

            if (Find(key, out var oldValue, out var oldIndex))
            {
                if (!overwrite || Equals(value, oldValue))
                    return this;

                newProperties = ReallocateArray(pairs.Length);
                newProperties[oldIndex] = newProperty;
                return new ImmutableArrayDictionary<TKey, TValue>(newProperties, Count, keyComparer);
            }

            if (pairs.Length == Count)
            {
                newProperties = ReallocateArray(Math.Max(DefaultCapacity, pairs.Length * 2));
                newProperties[Count] = newProperty;
                return new ImmutableArrayDictionary<TKey, TValue>(newProperties, Count + 1, keyComparer);
            }

            if (Interlocked.CompareExchange(ref pairs[Count], newProperty, null) != null)
            {
                newProperties = ReallocateArray(pairs.Length);
                newProperties[Count] = newProperty;
                return new ImmutableArrayDictionary<TKey, TValue>(newProperties, Count + 1, keyComparer);
            }

            return new ImmutableArrayDictionary<TKey, TValue>(pairs, Count + 1, keyComparer);
        }

        public ImmutableArrayDictionary<TKey, TValue> Remove(TKey key)
        {
            if (Find(key, out _, out var oldIndex))
            {
                var newProperties = new Pair[pairs.Length];

                Array.Copy(pairs, 0, newProperties, 0, oldIndex);
                Array.Copy(pairs, oldIndex + 1, newProperties, oldIndex, pairs.Length - oldIndex - 1);

                return new ImmutableArrayDictionary<TKey, TValue>(newProperties, Count - 1, keyComparer);
            }

            return this;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return pairs[i].ToKeyValuePair();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

            public KeyValuePair<TKey, TValue> ToKeyValuePair()
            {
                return new KeyValuePair<TKey, TValue>(Key, Value);
            }
        }
    }
}