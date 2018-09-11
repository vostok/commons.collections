using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class CircularBuffer_Tests
    {
        private CircularBuffer<int> buffer;

        [SetUp]
        public void TestSetup()
        {
            buffer = new CircularBuffer<int>(3);
        }

        [Test]
        public void Should_initially_represent_an_empty_enumerable()
        {
            buffer.Should().BeEmpty();
        }

        [Test]
        public void Should_initially_represent_an_empty_reverse_enumerable()
        {
            buffer.Reverse().Should().BeEmpty();
        }

        [Test]
        public void Add_should_append_new_elements_to_end_when_there_is_free_capacity()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.Should().Equal(1, 2, 3);
        }

        [Test]
        public void Add_should_overwrite_old_elements_when_capacity_is_exhausted()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            buffer.Should().Equal(3, 4, 5);
        }

        [Test]
        public void EnumerateReverse_should_return_correct_result_when_no_elements_were_overwritten()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.EnumerateReverse().Should().Equal(3, 2, 1);
        }

        [Test]
        public void EnumerateReverse_should_return_correct_result_when_some_elements_were_overwritten()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);
            buffer.Add(5);

            buffer.EnumerateReverse().Should().Equal(5, 4, 3);
        }

        [Test]
        public void Clear_should_reset_buffer_to_empty_state()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.Clear();

            buffer.Should().BeEmpty();
        }

        [Test]
        public void Last_property_should_produce_an_error_on_empty_buffer()
        {
            // ReSharper disable once NotAccessedVariable
            int last;

            Action action = () => last = buffer.Last;

            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void Last_property_should_return_last_item_for_non_overflowed_list()
        {
            buffer.Add(1);
            buffer.Add(2);

            buffer.Last.Should().Be(2);
        }

        [Test]
        public void Last_property_should_return_last_item_for_overflowed_list()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.Last.Should().Be(4);
        }

        [Test]
        public void First_property_should_produce_an_error_on_empty_buffer()
        {
            // ReSharper disable once NotAccessedVariable
            int first;

            Action action = () => first = buffer.First;

            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void First_property_should_return_first_item_for_non_overflowed_list()
        {
            buffer.Add(1);
            buffer.Add(2);

            buffer.First.Should().Be(1);
        }

        [Test]
        public void First_property_should_return_first_item_for_overflowed_list()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);
            buffer.Add(4);

            buffer.First.Should().Be(2);
        }

        [Test]
        public void IsFull_property_should_return_false_for_list_with_remaining_capacity()
        {
            buffer.Add(1);
            buffer.Add(2);

            buffer.IsFull.Should().BeFalse();
        }

        [Test]
        public void IsFull_property_should_return_true_for_list_without_remaining_capacity()
        {
            buffer.Add(1);
            buffer.Add(2);
            buffer.Add(3);

            buffer.IsFull.Should().BeTrue();
        }
    }
}