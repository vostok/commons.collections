﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class ConcurrentBoundedQueue_Tests
    {
        private const int Capacity = 5;
        private const int Batch = 2;

        private ConcurrentBoundedQueue<string> queue;
        private string[] drainResult;

        [SetUp]
        public void SetUp()
        {
            queue = new ConcurrentBoundedQueue<string>(Capacity, Batch);
            drainResult = new string[Capacity];
        }

        [Test]
        public void TryAdd_should_return_true_when_queue_has_free_space()
        {
            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString()).Should().BeTrue();
            }
        }

        [Test]
        public void TryAdd_should_return_false_when_queue_is_full()
        {
            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString());
            }

            queue.TryAdd(Capacity.ToString()).Should().BeFalse();
        }

        [Test]
        public void Drain_should_return_empty_array_when_queue_is_empty()
        {
            queue.Drain(drainResult, 0, drainResult.Length);
            drainResult.Should().Equal(Enumerable.Repeat((string)null, Capacity));
        }

        [TestCase(1, Capacity)]
        [TestCase(Capacity, 1)]
        public void Drain_should_return_correct_result(int addCount, int drainCount)
        {
            for (var i = 0; i < addCount; i++)
            {
                queue.TryAdd(i.ToString());
            }

            queue.Drain(drainResult, 0, drainCount);

            drainResult.Should().Equal(GenerateCorrectDrainResult(Math.Min(drainCount, addCount)));
        }

        [TestCase(1)]
        [TestCase(Capacity)]
        public void Drain_should_remove_items_from_queue(int count)
        {
            for (var i = 0; i < count; i++)
            {
                queue.TryAdd(i.ToString());
            }

            queue.Drain(new string[Capacity], 0, Capacity);
            queue.Drain(drainResult, 0, drainResult.Length);

            drainResult.Should().Equal(Enumerable.Repeat((string)null, Capacity));
        }

        [Test]
        public void Drain_should_make_room_for_new_items()
        {
            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString()).Should().BeTrue();
            }

            queue.Drain(drainResult, 0, drainResult.Length);

            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString()).Should().BeTrue();
            }
        }

        [Test]
        public void TryAdd_and_Drain_should_behave_correctly_on_random_workloads()
        {
            var random = new Random();

            for (var i = 0; i < 10 * 1000; i++)
            {
                var count = random.Next(Capacity + 1);

                for (var j = 0; j < count; j++)
                {
                    queue.TryAdd(j.ToString()).Should().BeTrue();
                }

                drainResult = new string[Capacity];
                queue.Drain(drainResult, 0, drainResult.Length);
                drainResult.Should().Equal(GenerateCorrectDrainResult(count));
            }
        }

        [Test]
        public void Drain_should_not_go_into_cycles_when_provided_array_is_larger_than_capacity()
        {
            drainResult = new string[Capacity * 2];

            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString());
            }

            var drainedItems = queue.Drain(drainResult, 0, drainResult.Length);

            drainedItems.Should().Be(Capacity);
        }

        [Test]
        public void WaitForNewItemsAsync_should_not_block_after_partial_drain()
        {
            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString()).Should().BeTrue();
            }

            queue.WaitForNewItemsAsync().Wait(1.Seconds()).Should().BeTrue();

            queue.Drain(drainResult, 0, 1);

            queue.WaitForNewItemsAsync().Wait(1.Seconds()).Should().BeTrue();
        }

        [Test]
        public void WaitForNewItemsAsync_should_return_after_an_event_is_added()
        {
            var task = queue.WaitForNewItemsAsync();

            task.IsCompleted.Should().BeFalse();

            queue.TryAdd("").Should().BeTrue();

            new Action(() => task.IsCompleted.Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void TryWaitForNewItemsAsync_should_not_reset_event_after_partial_drain()
        {
            for (var i = 0; i < Capacity; i++)
            {
                queue.TryAdd(i.ToString()).Should().BeTrue();
            }

            queue.TryWaitForNewItemsAsync(1.Seconds()).Result.Should().BeTrue();

            queue.Drain(drainResult, 0, 1);

            queue.TryWaitForNewItemsAsync(1.Seconds()).Result.Should().BeTrue();
        }

        [Test]
        public void TryWaitForNewItemsAsync_should_return_after_timeout()
        {
            var task = queue.TryWaitForNewItemsAsync(100.Milliseconds());

            task.IsCompleted.Should().BeFalse();

            new Action(() => (task.IsCompleted && !task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void TryWaitForNewItemsAsync_should_return_after_an_event_is_added()
        {
            var task = queue.TryWaitForNewItemsAsync(100.Milliseconds());

            task.IsCompleted.Should().BeFalse();

            queue.TryAdd("").Should().BeTrue();

            new Action(() => (task.IsCompleted && task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void TryWaitForNewItemsBatchAsync_should_return_after_timeout_without_items()
        {
            var task = queue.TryWaitForNewItemsBatchAsync(100.Milliseconds());

            task.IsCompleted.Should().BeFalse();

            new Action(() => (task.IsCompleted && !task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void TryWaitForNewItemsBatchAsync_should_return_after_timeout_without_enough_items()
        {
            var task = queue.TryWaitForNewItemsBatchAsync(1.Seconds());

            queue.TryAdd("").Should().BeTrue();

            new Action(() => task.IsCompleted.Should().BeFalse())
                .ShouldNotFailIn(0.5.Seconds());

            new Action(() => (task.IsCompleted && task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void TryWaitForNewItemsBatchAsync_should_return_after_batch_events_are_added()
        {
            var task = queue.TryWaitForNewItemsBatchAsync(100.Seconds());

            queue.TryAdd("").Should().BeTrue();

            new Action(() => task.IsCompleted.Should().BeFalse())
                .ShouldNotFailIn(0.5.Seconds());

            queue.TryAdd("").Should().BeTrue();

            new Action(() => (task.IsCompleted && task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [TestCase(4)]
        [TestCase(5)]
        public void TryWaitForNewItemsBatchAsync_should_return_if_there_are_still_batch_events(int items)
        {
            for (var i = 0; i < items; i++)
                queue.TryAdd("").Should().BeTrue();

            var task = queue.TryWaitForNewItemsBatchAsync(100.Seconds());
            new Action(() => (task.IsCompleted && task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());

            queue.Drain(drainResult, 0, 2).Should().Be(2);

            task = queue.TryWaitForNewItemsBatchAsync(100.Seconds());
            new Action(() => (task.IsCompleted && task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());

            queue.Drain(drainResult, 0, 2).Should().Be(2);
        }

        [TestCase(2)]
        [TestCase(3)]
        public void TryWaitForNewItemsBatchAsync_should_not_return_if_there_are_not_still_batch_events(int items)
        {
            for (var i = 0; i < items; i++)
                queue.TryAdd("").Should().BeTrue();

            var task = queue.TryWaitForNewItemsBatchAsync(1.Seconds());
            new Action(() => (task.IsCompleted && task.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());

            queue.Drain(drainResult, 0, 2).Should().Be(2);

            task = queue.TryWaitForNewItemsBatchAsync(1.Seconds());
            new Action(() => task.IsCompleted.Should().BeFalse())
                .ShouldNotFailIn(0.5.Seconds());
            new Action(() => (task.IsCompleted && task.Result == items - 2 > 0).Should().BeTrue())
                .ShouldPassIn(1.Seconds());

            queue.Drain(drainResult, 0, 2).Should().Be(items - 2);
        }

        [Test]
        public void TryWaitForNewItemsBatchAsync_may_return_if_less_than_batch_drained()
        {
            queue.TryAdd("").Should().BeTrue();
            queue.TryAdd("").Should().BeTrue();

            var task1 = queue.TryWaitForNewItemsBatchAsync(100.Seconds());
            queue.Drain(drainResult, 0, 1).Should().Be(1);
            var task2 = queue.TryWaitForNewItemsBatchAsync(100.Seconds());

            // note (kungurtsev, 20.07.2022): task is finished, but there isn't batch items
            new Action(() => (task1.IsCompleted && task1.Result).Should().BeTrue())
                .ShouldPassIn(1.Seconds());
            // note (kungurtsev, 20.07.2022): but newer task is running:
            new Action(() => task2.IsCompleted.Should().BeFalse())
                .ShouldNotFailIn(1.Seconds());
        }

        private IEnumerable<string> GenerateCorrectDrainResult(int count)
        {
            count = Math.Min(count, drainResult.Length);
            return Enumerable.Range(0, count).Select(j => j.ToString()).Concat(Enumerable.Repeat((string)null, drainResult.Length - count));
        }
    }
}