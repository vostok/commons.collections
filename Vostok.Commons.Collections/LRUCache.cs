using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    [PublicAPI]
    internal class LRUCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly int capacity;
        private readonly IEqualityComparer<TKey> comparer;

        private readonly LinkedList<(TKey key, TValue value)> queue;
        private readonly ConcurrentDictionary<TKey, LinkedListNode<(TKey key, TValue value)>> map;

        public LRUCache(int capacity, [CanBeNull] IEqualityComparer<TKey> comparer = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Cache capacity must be positive");

            this.capacity = capacity;
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;

            queue = new LinkedList<(TKey, TValue)>();
            map = new ConcurrentDictionary<TKey, LinkedListNode<(TKey, TValue)>>(this.comparer);
        }

        public int Count => map.Count;

        public bool TryGet(TKey key, out TValue value)
        {
            if (!map.TryGetValue(key, out var node))
            {
                value = default;
                return false;
            }

            value = node.Value.value;

            RepositionNode();

            return true;

            void RepositionNode()
            {
                if (node.List == null)
                    return;

                lock (queue)
                {
                    if (node.List == null)
                        return;

                    queue.Remove(node);
                    queue.AddLast(node);
                }
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            => GetOrAddAsync(key, k => Task.FromResult(valueFactory(k))).GetAwaiter().GetResult();

        public async Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> valueFactory)
        {
            while (true)
            {
                if (TryGet(key, out var value))
                    return value;

                var node = new LinkedListNode<(TKey key, TValue value)>((key, await valueFactory(key)));

                if (!map.TryAdd(key, node))
                    continue;

                LinkedListNode<(TKey key, TValue value)> first = null;

                lock (queue)
                {
                    if (queue.Count >= capacity)
                    {
                        first = queue.First;
                        queue.RemoveFirst();
                    }

                    queue.AddLast(node);
                }

                if (first != null)
                    map.TryRemove(first.Value.key, out _);

                return node.Value.value;
            }
        }

        public void Remove(TKey key)
        {
            if (!map.TryRemove(key, out var node))
                return;

            lock (queue)
            {
                if (node.List == null)
                    return;

                queue.Remove(node);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => 
            map.Select(pair => new KeyValuePair<TKey, TValue>(pair.Value.Value.key, pair.Value.Value.value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
