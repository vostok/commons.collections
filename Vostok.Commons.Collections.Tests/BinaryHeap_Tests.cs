using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture(true)]
    [TestFixture(false)]
    internal class BinaryHeap_Tests
    {
        private readonly bool isMaxHeap;
        private readonly Random random = new Random();

        private BinaryHeap<int> heap;

        public BinaryHeap_Tests(bool isMaxHeap)
        {
            this.isMaxHeap = isMaxHeap;
        }

        [SetUp]
        public void TestSetup()
        {
            heap = new BinaryHeap<int>(isMaxHeap: isMaxHeap);
        }

        [Test]
        public void Peek_should_fail_when_heap_is_empty()
        {
            Action action = () => heap.Peek();

            action.Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void Peek_should_return_minimum_or_maximum_element()
        {
            var items = GenerateItems();

            FillHeap(items);

            heap.Peek().Should().Be(isMaxHeap ? items.Max() : items.Min());
        }

        [Test]
        public void Peek_should_not_remove_the_element()
        {
            FillHeap(GenerateItems());

            var countBefore = heap.Count;
            var result1 = heap.Peek();
            var result2 = heap.Peek();
            var result3 = heap.Peek();
            var countAfter = heap.Count;

            countAfter.Should().Be(countBefore);
            result2.Should().Be(result1);
            result3.Should().Be(result1);
        }

        [Test]
        public void RemoveRoot_should_return_minimum_or_maximum_elements()
        {
            var items = GenerateItems();

            FillHeap(items);

            var drainedItems = DrainHeap().ToArray();

            drainedItems.Should().BeEquivalentTo(items);

            if (isMaxHeap)
            {
                drainedItems.Should().BeInDescendingOrder();
            }
            else
            {
                drainedItems.Should().BeInAscendingOrder();
            }
        }

        [Test]
        public void RemoveRoot_should_remove_the_element()
        {
            FillHeap(GenerateItems());

            var count = heap.Count;

            foreach (var unused in DrainHeap())
            {
                heap.Count.Should().Be(--count);
            }
        }

        private IEnumerable<int> DrainHeap()
        {
            while (heap.Count > 0)
            {
                yield return heap.RemoveRoot();
            }
        }

        private void FillHeap(IEnumerable<int> items)
        {
            foreach (var item in items)
            {
                heap.Add(item);
            }
        }

        private List<int> GenerateItems()
        {
            var count = random.Next(1, 100);

            var result = new List<int>(count);

            for (var i = 0; i < count; i++)
            {
                result.Add(random.Next(50));
            }

            return result;
        }
    }
}