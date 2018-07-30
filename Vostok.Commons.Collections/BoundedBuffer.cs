using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Commons.Collections
{
    internal class BoundedBuffer<T>
        where T : class
    {
        private readonly T[] items;

        private TaskCompletionSource<bool> canDrainAsync;
        private int itemsCount;
        private int frontPtr;
        private volatile int backPtr;

        public BoundedBuffer(int capacity)
        {
            items = new T[capacity];
            canDrainAsync = new TaskCompletionSource<bool>();
        }

        public int Count => itemsCount;

        public bool TryAdd(T item)
        {
            while (true)
            {
                var currentCount = itemsCount;
                if (currentCount >= items.Length)
                    return false;

                if (Interlocked.CompareExchange(ref itemsCount, currentCount + 1, currentCount) == currentCount)
                {
                    while (true)
                    {
                        var currentFrontPtr = frontPtr;

                        if (Interlocked.CompareExchange(ref frontPtr, (currentFrontPtr + 1)%items.Length, currentFrontPtr) == currentFrontPtr)
                        {
                            items[currentFrontPtr] = item;

                            canDrainAsync.TrySetResult(true);

                            return true;
                        }
                    }
                }
            }
        }

        public int Drain(T[] buffer, int index, int count)
        {
            if (itemsCount == 0)
                return 0;

            canDrainAsync = new TaskCompletionSource<bool>();

            var resultCount = 0;

            for (var i = 0; i < count; i++)
            {
                var itemIndex = (backPtr + i)%items.Length;
                var item = Interlocked.Exchange(ref items[itemIndex], null);
                if (item == null)
                    break;

                buffer[index + resultCount++] = item;
            }

            backPtr = (backPtr + resultCount)%items.Length;

            Interlocked.Add(ref itemsCount, -resultCount);

            return resultCount;
        }

        public Task WaitForNewItemsAsync() => canDrainAsync.Task;
    }
}