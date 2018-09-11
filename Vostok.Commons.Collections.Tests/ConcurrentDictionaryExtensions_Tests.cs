using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class ConcurrentDictionaryExtensions_Tests
    {
        private ConcurrentDictionary<string, int> dictionary;

        [SetUp]
        public void TestSetup()
        {
            dictionary = new ConcurrentDictionary<string, int>
            {
                ["key1"] = 1,
                ["key2"] = 2,
                ["key3"] = 3
            };
        }

        [Test]
        public void Remove_with_value_should_not_remove_anything_when_value_does_not_match()
        {
            dictionary.Remove("key1", 2).Should().BeFalse();

            dictionary.Should().HaveCount(3);
        }

        [Test]
        public void Remove_with_value_should_succeed_when_value_matches()
        {
            dictionary.Remove("key1", 1).Should().BeTrue();

            dictionary.Should().HaveCount(2);

            dictionary.Should().NotContainKey("key1");
        }
    }
}