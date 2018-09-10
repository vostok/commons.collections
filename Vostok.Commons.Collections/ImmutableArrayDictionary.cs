using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    /// <summary>
    /// <para>An immutable dictionary implemented using an array.</para>
    /// <para>Search takes linear time, so this collection should only be used with small number of items.</para>
    /// </summary>
    [PublicAPI]
    internal class ImmutableArrayDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private const int DefaultCapacity = 4;

        /// <summary>
        /// An empty <see cref="ImmutableArrayDictionary{TKey,TValue}"/>.
        /// </summary>
        public static readonly ImmutableArrayDictionary<TKey, TValue> Empty = new ImmutableArrayDictionary<TKey, TValue>(0);

        private readonly Pair[] pairs;

        private readonly IEqualityComparer<TKey> keyComparer;

        /// <summary>
        /// Create a new <see cref="ImmutableArrayDictionary{TKey,TValue}"/> with default capacity.
        /// </summary>
        public ImmutableArrayDictionary([CanBeNull] IEqualityComparer<TKey> keyComparer = null)
            : this(DefaultCapacity, keyComparer)
        {
        }

        /// <summary>
        /// Create a new <see cref="ImmutableArrayDictionary{TKey,TValue}"/> with the given capacity.
        /// </summary>
        public ImmutableArrayDictionary(int capacity, [CanBeNull] IEqualityComparer<TKey> keyComparer = null)
            : this(new Pair[capacity], 0, keyComparer)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "The capacity must be non-negative");
        }

        private ImmutableArrayDictionary(Pair[] pairs, int count, IEqualityComparer<TKey> keyComparer)
        {
            this.pairs = pairs;
            this.keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

            Count = count;
        }

        /// <inheritdoc />
        public int Count { get; }

        /// <inheritdoc />
        public IEnumerable<TKey> Keys =>
            this.Select(pair => pair.Key);

        /// <inheritdoc />
        public IEnumerable<TValue> Values =>
            this.Select(pair => pair.Value);

        /// <inheritdoc />
        public bool ContainsKey(TKey key) =>
            Find(key, out _, out _);

        /// <inheritdoc />
        public bool TryGetValue(TKey key, out TValue value) =>
            Find(key, out value, out _);

        /// <summary>
        /// Returns a new <see cref="ImmutableArrayDictionary{TKey,TValue}"/> with the same data plus <paramref name="key"/> set to <paramref name="value"/>.
        /// </summary>
        /// <param name="overwrite">Specifies the behavior in case a value with the same key exists. If <c>true</c>, the value will be overwritten in the returned dictionary. Otherwise, the new value is ignored and the original dictionary is returned.</param>
        public ImmutableArrayDictionary<TKey, TValue> Set(TKey key, TValue value, bool overwrite = true)
        {
            Pair[] newProperties;

            var hash = ComputeHash(key);
            
            var newProperty = new Pair(key, value, hash);

            if (Find(key, hash, out var oldValue, out var oldIndex))
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

        /// <summary>
        /// Returns a new <see cref="ImmutableArrayDictionary{TKey,TValue}"/> with the same data except the value by <paramref name="key"/>. If there is no such key, the original dictionary is returned.
        /// </summary>
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

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return pairs[i].ToKeyValuePair();
        }

        /// <inheritdoc />
        public TValue this[TKey key] =>
            Find(key, out var value, out _) ? value : throw new KeyNotFoundException($"A value with key '{key}' is not present.");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int ComputeHash(TKey key) => key == null ? 0 : keyComparer.GetHashCode(key);

        private bool Find(TKey key, out TValue value, out int index)
            => Find(key, ComputeHash(key), out value, out index); 
        
        private bool Find(TKey key, int hash, out TValue value, out int index)
        {
            for (var i = 0; i < Count; i++)
            {
                var property = pairs[i];
                if (hash == property.Hash && keyComparer.Equals(property.Key, key))
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
            public Pair(TKey key, TValue value, int hash)
            {
                Key = key;
                Value = value;
                Hash = hash;
            }

            public TKey Key { get; }
            public TValue Value { get; }
            public int Hash { get; }

            public KeyValuePair<TKey, TValue> ToKeyValuePair() =>
                new KeyValuePair<TKey, TValue>(Key, Value);
        }
    }
}