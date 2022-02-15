#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vostok.Commons.Collections
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> factory)
        {
            if (dict.TryGetValue(key, out var value))
                return value;
            return dict[key] = factory(key);
        }

        public static DictionariesDiff<TKey> GetDiff<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> left,
            IReadOnlyDictionary<TKey, TValue> right,
            IEqualityComparer<TValue> valueComparer)
            where TKey : notnull => left.GetDiff(right, valueComparer.Equals);
        
        public static DictionariesDiff<TKey> GetDiff<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> left,
            IReadOnlyDictionary<TKey, TValue> right,
            Func<TValue, TValue, bool>? valueComparer = null)
            where TKey : notnull
        {
            valueComparer ??= (a, b) => ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
            
            var inLeft = new HashSet<TKey>();
            var changed = new HashSet<TKey>();
            var same = new HashSet<TKey>();
            var inRight = new HashSet<TKey>(right.Keys.Where(k => !left.ContainsKey(k)));

            foreach (var lPair in left)
            {
                var collection = right.TryGetValue(lPair.Key, out var rValue)
                    ? (valueComparer(lPair.Value, rValue) ? same : changed)
                    : inLeft;
                
                collection.Add(lPair.Key);
            }

            return new(inLeft, changed, same, inRight);
        }
    }
}