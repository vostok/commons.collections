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

        public UnboundedObjectPool([NotNull] Func<T> itemFactory)
        {
            this.itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
        }

        public T Acquire() =>
            items.TryDequeue(out var item) ? item : itemFactory();

        public IDisposable Acquire(out T item) =>
            new Releaser(item = Acquire(), this);

        public void Return(T item) =>
            items.Enqueue(item);

        private class Releaser : IDisposable
        {
            private readonly T item;
            private readonly UnboundedObjectPool<T> pool;

            public Releaser(T item, UnboundedObjectPool<T> pool)
            {
                this.item = item;
                this.pool = pool;
            }

            public void Dispose()
            {
                pool.Return(item);
            }
        }
    }
}