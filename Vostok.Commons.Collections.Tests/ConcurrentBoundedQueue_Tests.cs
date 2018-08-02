using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class ConcurrentBoundedQueue_Tests
    {
        private const int Capacity = 5;

        private ConcurrentBoundedQueue<string> queue;
        private string[] drainResult;

        [SetUp]
        public void SetUp()
        {
            queue = new ConcurrentBoundedQueue<string>(Capacity);
            drainResult = new string[Capacity];
        }

        [Test]
        public void TryAdd_should_return_true_when_queue_has_free_space()
        {
            for (var i = 0; i < Capacity; i++)
                queue.TryAdd(i.ToString()).Should().BeTrue();
        }

        [Test]
        public void TryAdd_should_return_false_when_queue_is_full()
        {
            for (var i = 0; i < Capacity; i++)
                queue.TryAdd(i.ToString());

            queue.TryAdd(Capacity.ToString()).Should().BeFalse();
        }

        [Test]
        public void Drain_should_return_empty_array_when_queue_is_empty()
        {
            queue.Drain(drainResult, 0, drainResult.Length);
            drainResult.Should().Equal(Enumerable.Repeat((string) null, Capacity));
        }

        [TestCase(1, Capacity)]
        [TestCase(Capacity, 1)]
        public void Drain_should_return_correct_result(int addCount, int drainCount)
        {
            for (var i = 0; i < addCount; i++)
                queue.TryAdd(i.ToString());

            queue.Drain(drainResult, 0, drainCount);

            drainResult.Should().Equal(GenerateCorrectDrainResult(Math.Min(drainCount, addCount)));
        }

        [TestCase(1)]
        [TestCase(Capacity)]
        public void Drain_should_remove_items_from_queue(int count)
        {
            for (var i = 0; i < count; i++)
                queue.TryAdd(i.ToString());

            queue.Drain(new string[Capacity], 0, Capacity);
            queue.Drain(drainResult, 0, drainResult.Length);

            drainResult.Should().Equal(Enumerable.Repeat((string) null, Capacity));
        }

        [Test]
        public void Drain_should_make_room_for_new_items()
        {
            for (var i = 0; i < Capacity; i++)
                queue.TryAdd(i.ToString()).Should().BeTrue();

            queue.Drain(drainResult, 0, drainResult.Length);

            for (var i = 0; i < Capacity; i++)
                queue.TryAdd(i.ToString()).Should().BeTrue();
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

        private IEnumerable<string> GenerateCorrectDrainResult(int count)
        {
            count = Math.Min(count, drainResult.Length);
            return Enumerable.Range(0, count).Select(j => j.ToString()).Concat(Enumerable.Repeat((string) null, drainResult.Length - count));
        }
    }
}
