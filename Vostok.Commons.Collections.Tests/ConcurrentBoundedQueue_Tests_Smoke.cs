using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

// ReSharper disable MethodSupportsCancellation

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    public class ConcurrentBoundedQueue_Tests_Smoke
    {
        [Explicit]
        [TestCase(1000, 4)]
        [TestCase(1000, 4, true)]
        [TestCase(1000, 1)]
        [TestCase(1000, 1, true)]
        [TestCase(2, 4)]
        [TestCase(2, 40)]
        [TestCase(1, 4)]
        public void All_successfully_added_items_should_be_eventually_consumed(int capacity, int writersCount, bool useBatch = false)
        {
            var addedItemsCount = 0;
            var drainedItemsCount = 0;
            var (drainedFullBatches, drainedNonFullBatches) = (0, 0);
            var buffer = new object[10];
            var drainBatchCount = Math.Min(capacity, buffer.Length);
            var queue = new ConcurrentBoundedQueue<object>(capacity, drainBatchCount);
            var cancellation = new CancellationTokenSource();
            var cancellationToken = cancellation.Token;

            var trigger = new CountdownEvent(writersCount + 1);
            var writers = Enumerable.Range(0, writersCount)
                .Select(
                    _ => Task.Run(
                        () =>
                        {
                            trigger.Signal();
                            trigger.Wait();

                            while (!cancellationToken.IsCancellationRequested)
                            {
                                var item = new object();
                                if (queue.TryAdd(item))
                                    Interlocked.Increment(ref addedItemsCount);
                            }
                        }))
                .ToArray();

            var reader = Task.Run(
                async () =>
                {
                    trigger.Signal();
                    trigger.Wait();
                    while (!cancellation.IsCancellationRequested || writers.Any(w => !w.IsCompleted) || queue.Count > 0)
                    {
                        var tryWaitForNewItemsAsync = useBatch
                            ? queue.TryWaitForNewItemsBatchAsync(10.Milliseconds(), 100.Milliseconds()).ConfigureAwait(false)
                            : queue.TryWaitForNewItemsAsync(100.Milliseconds()).ConfigureAwait(false);
                        if (!await tryWaitForNewItemsAsync)
                        {
                            if (writers.Any(w => !w.IsCompleted))
                                throw new Exception("Wait seems to be stuck.");
                        }

                        var count = queue.Drain(buffer, 0, buffer.Length);
                        if (count < drainBatchCount)
                            drainedNonFullBatches++;
                        else
                            drainedFullBatches++;
                        
                        drainedItemsCount += count;
                    }
                });

            Thread.Sleep(10.Seconds());

            cancellation.Cancel();

            Task.WaitAll(writers);

            reader.Wait();

            Console.WriteLine($"added: {addedItemsCount}, drained: {drainedItemsCount}");
            Console.WriteLine($"full bathes: {drainedFullBatches}, non full bathes: {drainedNonFullBatches} ({100.0 * drainedNonFullBatches/(drainedNonFullBatches + drainedFullBatches)}%)");

            queue.Count.Should().Be(0);
            drainedItemsCount.Should().Be(addedItemsCount);
        }

        [Test]
        public void Should_not_have_race_between_Add_and_Drain_that_turns_queue_wait_task_to_inconsistent_state()
        {
            for (var j = 0; j < 10; ++j)
            {
                var queue = new ConcurrentBoundedQueue<object>(100, 10);
                var o = new object();

                var addTask = Task.Run(
                    () =>
                    {
                        for (var i = 0; i < 1000000; i++)
                        {
                            if (!queue.TryAdd(o))
                                i--;
                        }
                    });

                var drainTask = Task.Run(
                    () =>
                    {
                        var arr = new object[10];
                        while (true)
                        {
                            var drained = queue.Drain(arr, 0, arr.Length);
                            if (drained == 0 && addTask.IsCompleted)
                                return;
                        }
                    });

                Task.WhenAll(drainTask, addTask).GetAwaiter().GetResult();

                if (queue.WaitForNewItemsAsync().IsCompleted && queue.Count == 0)
                    Assert.Fail("Queue is broken: TryWaitForNewItemsAsync is completed, but queue is empty.");
                if (queue.WaitForNewItemsBatchAsync().IsCompleted && queue.Count < 10)
                    Assert.Fail("Queue is broken: TryWaitForNewItemsBatchAsync is completed, but items is not enough.");
            }
        }
    }
}