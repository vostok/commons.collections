using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class RecyclingBoundedCache_Tests
    {
        private RecyclingBoundedCache<string, int> cache;

        [SetUp]
        public void TestSetup()
        {
            cache = new RecyclingBoundedCache<string, int>(3);
        }

        [Test]
        public void Obtain_should_cache_first_observed_value_and_serve_it_on_later_invocations()
        {
            cache.Obtain("key", _ => 1).Should().Be(1);
            cache.Obtain("key", _ => 2).Should().Be(1);
            cache.Obtain("key", _ => 3).Should().Be(1);
        }

        [Test]
        public void Obtain_should_not_clean_cache_until_capacity_is_exceeded()
        {
            cache.Obtain("key1", _ => 1).Should().Be(1);
            cache.Obtain("key2", _ => 1).Should().Be(1);
            cache.Obtain("key3", _ => 1).Should().Be(1);

            cache.Obtain("key1", _ => 2).Should().Be(1);
            cache.Obtain("key2", _ => 2).Should().Be(1);
            cache.Obtain("key3", _ => 2).Should().Be(1);
        }

        [Test]
        public void Obtain_should_wipe_the_cache_when_capacity_is_exceeded()
        {
            cache.Obtain("key1", _ => 1).Should().Be(1);
            cache.Obtain("key2", _ => 1).Should().Be(1);
            cache.Obtain("key3", _ => 1).Should().Be(1);
            cache.Obtain("key4", _ => 1).Should().Be(1);

            cache.Obtain("key1", _ => 2).Should().Be(2);
            cache.Obtain("key2", _ => 2).Should().Be(2);
            cache.Obtain("key3", _ => 2).Should().Be(2);
        }
    }
}