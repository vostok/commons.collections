using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    [PublicAPI]
#if MAKE_CLASSES_PUBLIC
    public
#else
    internal
#endif
    class UnboundedObjectPool<T>
    {
        private readonly ConcurrentQueue<T> items = new ConcurrentQueue<T>();
        private readonly Func<T> itemFactory;

        public UnboundedObjectPool(Func<T> itemFactory)
        {
            this.itemFactory = itemFactory;
        }

        public T Acquire()
        {
            return items.TryDequeue(out var item) ? item : itemFactory();
        }

        public void Return(T item)
        {
            items.Enqueue(item);
        }
    }
}