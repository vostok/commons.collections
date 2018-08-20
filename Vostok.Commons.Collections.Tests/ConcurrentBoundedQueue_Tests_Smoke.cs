using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [Explicit]
    [TestFixture]
    public class ConcurrentBoundedQueue_Tests_Smoke
    {
        [TestCase(1000, 4)]
        [TestCase(1000, 1)]
        [TestCase(2, 4)]
        [TestCase(2, 40)]
        [TestCase(1, 4)]
        public void All_successfully_added_items_should_be_eventually_consumed(int capacity, int writersCount)
        {
            var stop = false;
            var addedItemsCount = 0;
            var drainedItemsCount = 0;
            var queue = new ConcurrentBoundedQueue<object>(capacity);

            var trigger = new CountdownEvent(writersCount + 1);
            var writers = Enumerable.Range(0, writersCount).Select(_ => Task.Run(
                () =>
                {
                    trigger.Signal();
                    trigger.Wait();
                    while (!stop)
                    {
                        var item = new object();
                        if (queue.TryAdd(item))
                            Interlocked.Increment(ref addedItemsCount);
                    }
                })).ToArray();

            var reader = Task.Run(
                async () =>
                {
                    trigger.Signal();
                    trigger.Wait();
                    var buffer = new object[10];
                    while (!stop || queue.Count > 0 || writers.Any(w => !w.IsCompleted))
                    {
                        var x = queue.WaitForNewItemsAsync();
                        if (await Task.WhenAny(x, Task.Delay(1000)) != x)
                        {
                            if (writers.Any(w => !w.IsCompleted))
                                throw new Exception("Wait seems to be stuck.");
                        }
                        var count = queue.Drain(buffer, 0, buffer.Length);
                        drainedItemsCount += count;
                    }
                });

            Thread.Sleep(10.Seconds());

            stop = true;
            Task.WaitAll(writers);
            reader.Wait();

            queue.Count.Should().Be(0);
            drainedItemsCount.Should().Be(addedItemsCount);
        }
    }
}