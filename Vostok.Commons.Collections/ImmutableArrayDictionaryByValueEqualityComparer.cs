using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    [PublicAPI]
    internal class ImmutableArrayDictionaryByValueEqualityComparer<TKey, TValue> : IEqualityComparer<ImmutableArrayDictionary<TKey, TValue>>
    {
        public static readonly ImmutableArrayDictionaryByValueEqualityComparer<TKey, TValue> Instance = new();

        public bool Equals(ImmutableArrayDictionary<TKey, TValue> x, ImmutableArrayDictionary<TKey, TValue> y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            if (x.GetType() != y.GetType())
                return false;
            if (x.Count != y.Count)
                return false;

            foreach (var pair in x)
            {
                if (!y.TryGetValue(pair.Key, out var yValue)) return false;
                if (!EqualityComparer<TValue>.Default.Equals(pair.Value, yValue)) return false;
            }
        
            return true;
        }

        public int GetHashCode(ImmutableArrayDictionary<TKey, TValue> obj)
        {
            var hKeys = 0;
            var hValues = 0;
            foreach (var pair in obj.OrderBy(kvp => kvp.Key))
            {
                hKeys ^= pair.Key.GetHashCode();
                hValues ^= pair.Value.GetHashCode();
            }

#if NETSTANDARD2_0
            var hash = 23;
            hash = unchecked(hash * 31 + hKeys);
            hash = unchecked(hash * 31 + hValues);
            return hash;
#else
            return HashCode.Combine(hKeys, hValues);
#endif
        }

        private ImmutableArrayDictionaryByValueEqualityComparer()
        {
        }
    }
}