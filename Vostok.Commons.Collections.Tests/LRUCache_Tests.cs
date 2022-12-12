using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class LRUCache_Tests
    {
        private LRUCache<string, int> cache;

        [SetUp]
        public void TestSetup()
            => cache = new LRUCache<string, int>(3);

        [Test]
        public void GetOrAdd_should_return_cached_values()
        {
            cache.GetOrAdd("key", _ => 1).Should().Be(1);
            cache.GetOrAdd("key", _ => 2).Should().Be(1);
            cache.GetOrAdd("key", _ => 3).Should().Be(1);
        }

        [Test]
        public void GetOrAdd_should_evict_least_recently_used()
        {
            cache.GetOrAdd("key1", _ => 1).Should().Be(1);
            cache.GetOrAdd("key2", _ => 2).Should().Be(2);
            cache.GetOrAdd("key3", _ => 3).Should().Be(3);

            cache.TryGet("key2", out _).Should().BeTrue();
            cache.TryGet("key1", out _).Should().BeTrue();
            cache.TryGet("key3", out _).Should().BeTrue();

            cache.GetOrAdd("key3", _ => 4).Should().Be(3);
            cache.GetOrAdd("key4", _ => 5).Should().Be(5);
            cache.GetOrAdd("key5", _ => 6).Should().Be(6);

            cache.Select(pair => pair.Key).Should().BeEquivalentTo("key3", "key4", "key5");
        }
    }
}