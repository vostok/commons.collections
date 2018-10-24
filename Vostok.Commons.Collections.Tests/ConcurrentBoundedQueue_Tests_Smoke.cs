using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    public class ConcurrentBoundedQueue_Tests_Smoke
    {
        [Explicit]
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
                    while (!stop || writers.Any(w => !w.IsCompleted) || queue.Count > 0)
                    {
                        if (!await queue.TryWaitForNewItemsAsync(100.Milliseconds()).ConfigureAwait(false))
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

            Console.WriteLine($"added: {addedItemsCount}, drained: {drainedItemsCount}");

            queue.Count.Should().Be(0);
            drainedItemsCount.Should().Be(addedItemsCount);
        }

        [Test]
        public void Should_not_have_race_between_Add_and_Drain_that_turns_queue_wait_task_to_inconsistent_state()
        {
            for (var j = 0; j < 10; ++j)
            {
                var queue = new ConcurrentBoundedQueue<object>(100);
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
                        var arr = new object[1000];
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
            }
        }
    }
}