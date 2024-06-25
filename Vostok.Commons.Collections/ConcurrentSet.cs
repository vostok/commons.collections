using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Vostok.Commons.Collections
{
    public class ConcurrentSet<T> : ICollection<T>
    {
        public ConcurrentSet()
        {
            dictionary = new ConcurrentDictionary<T, byte>();
        }

        public ConcurrentSet(IEqualityComparer<T> comparer)
        {
            dictionary = new ConcurrentDictionary<T, byte>(comparer);
        }

        public ConcurrentSet(int concurrencyLevel, int capacity)
        {
            dictionary = new ConcurrentDictionary<T, byte>(concurrencyLevel, capacity);
        }

        public ConcurrentSet(IEnumerable<T> items)
            : this()
        {
            foreach (var item in items)
                Add(item);
        }

        public bool Add(T item)
        {
            return dictionary.TryAdd(item, 0);
        }

        public bool Contains(T item)
        {
            return dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            dictionary.Keys.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            byte value;
            return dictionary.TryRemove(item, out value);
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Select(pair => pair.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public bool IsReadOnly => false;

        private readonly ConcurrentDictionary<T, byte> dictionary;
    }
}