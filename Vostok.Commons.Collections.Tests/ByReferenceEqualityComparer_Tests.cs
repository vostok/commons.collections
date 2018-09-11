using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class ByReferenceEqualityComparer_Tests
    {
        private ByReferenceEqualityComparer<string> comparer;

        [SetUp]
        public void TestSetup()
        {
            comparer = new ByReferenceEqualityComparer<string>();
        }

        [Test]
        public void Equals_should_return_true_for_references_pointing_to_same_object()
        {
            var value = "value";
            var value1 = value;
            var value2 = value;

            comparer.Equals(value1, value2).Should().BeTrue();
        }

        [Test]
        public void Equals_should_return_false_for_equal_but_not_same_objects()
        {
            var value = "value";
            var value1 = new string(value.ToCharArray());
            var value2 = new string(value.ToCharArray());

            comparer.Equals(value1, value2).Should().BeFalse();
        }

        [Test]
        public void GetHashCode_should_return_same_values_for_references_pointing_to_same_object()
        {
            var value = "value";
            var value1 = value;
            var value2 = value;

            comparer.GetHashCode(value1).Should().Be(comparer.GetHashCode(value2));
        }

        [Test]
        public void GetHashCode_should_return_different_values_for_equal_but_not_same_objects()
        {
            var value = "value";
            var value1 = new string(value.ToCharArray());
            var value2 = new string(value.ToCharArray());

            comparer.GetHashCode(value1).Should().NotBe(comparer.GetHashCode(value2));
        }
    }
}