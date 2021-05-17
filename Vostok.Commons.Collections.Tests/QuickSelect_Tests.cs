using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class QuickSelect_Tests
    {
        [TestCase(QuickselectSortOrder.Ascending)]
        [TestCase(QuickselectSortOrder.Descending)]
        public void Should_move_top_items_to_the_beginning_of_given_array(QuickselectSortOrder order)
        {
            var random = new Random();
            
            var itemsCount = random.Next(100, 200);
            var items = new int[itemsCount];
            for (var i = 1; i <= itemsCount - 1; i++)
            {
                for (var j = 0; j < itemsCount; j++)
                {
                    items[j] = random.Next(10);
                }

                var result = items.QuickSelect(i, order: order);
                AssertQuickSortResult(order, items, i, result);
            }
        }

        [TestCase(QuickselectSortOrder.Ascending)]
        [TestCase(QuickselectSortOrder.Descending)]
        public void Should_select_fast_on_sameData(QuickselectSortOrder order)
        {
            var itemsCount = 8000;
            var items = Enumerable.Repeat(new string('c', 10), itemsCount).ToArray();
            var comparer = new CountedComparer<string>(Comparer<string>.Default);
            var result = items.QuickSelect(6000, order: order, comparer: comparer);
            comparer.Called.Should().BeLessThan(itemsCount * 7); // Avg Performace O(n)
            AssertQuickSortResult(order, items, 6000, result);
        }

        [Test]
        public void Should_select_fast_on_sortedArray()
        {
            var itemsCount = 8000;
            var items = Enumerable.Range(0, itemsCount).ToArray();
            var comparer = new CountedComparer<int>(Comparer<int>.Default);
            var result = items.QuickSelect(6000, order: QuickselectSortOrder.Ascending, comparer: comparer);
            comparer.Called.Should().BeLessThan(itemsCount * 7); // Avg Performace O(n)
            AssertQuickSortResult(QuickselectSortOrder.Ascending, items, 6000, result);
        }

        [Test]
        public void Should_select_fast_on_reverseSortedArray()
        {
            var itemsCount = 8000;
            var items = Enumerable.Range(0, itemsCount).ToArray();
            var comparer = new CountedComparer<int>(Comparer<int>.Default);
            var result = items.QuickSelect(6000, order: QuickselectSortOrder.Descending, comparer: comparer);
            comparer.Called.Should().BeLessThan(itemsCount * 7);// Avg Performace O(n)
            AssertQuickSortResult(QuickselectSortOrder.Descending, items, 6000, result);
        }

        [TestCase(QuickselectSortOrder.Ascending)]
        [TestCase(QuickselectSortOrder.Descending)]
        public void Should_select_fast_on_similarData(QuickselectSortOrder order)
        {
            var itemsCount = 800;
            var items = new[] { "c", "a", "b", "zf", "e", "f", "w", "we", "z", "gg" }
                .SelectMany(prefix => Enumerable.Repeat(prefix + new string('c', 10), itemsCount))
                .ToArray();
            var comparer = new CountedComparer<string>(Comparer<string>.Default);
            var result = items.QuickSelect(6000, order: order, comparer: comparer);
            comparer.Called.Should().BeLessThan(items.Length * 7);// Avg Performace O(n)
            AssertQuickSortResult(order, items, 6000, result);
        }

        private static void AssertQuickSortResult<TItem>(QuickselectSortOrder order, TItem[] items, int i, TItem result)
        {
            var cmp = Comparer<TItem>.Default;
            if (order == QuickselectSortOrder.Ascending)
            {
                var minimum = items.Skip(i).Min();
                foreach (var item in items.Take(i))
                {
                    cmp.Compare(item, minimum).Should().BeLessOrEqualTo(0);
                }

                cmp.Compare(minimum, result).Should().Be(0);
            }
            else
            {
                var maximum = items.Skip(i).Max();
                foreach (var item in items.Take(i))
                {
                    cmp.Compare(item, maximum).Should().BeGreaterOrEqualTo(0);
                }
                
                cmp.Compare(maximum, result).Should().Be(0);
            }
        }

        private class CountedComparer<T> : IComparer<T>
        {
            private readonly IComparer<T> comparerImplementation;

            public CountedComparer(IComparer<T> comparerImplementation)
            {
                this.comparerImplementation = comparerImplementation;
            }

            public int Called { get; private set; }

            public int Compare(T x, T y)
            {
                Called++;
                return comparerImplementation.Compare(x, y);
            }
        }
    }
}