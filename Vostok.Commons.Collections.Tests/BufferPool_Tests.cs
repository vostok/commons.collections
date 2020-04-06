using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    internal class BufferPool_Tests
    {
        private readonly BufferPool pool = new BufferPool(maxArraysPerBucket: 5);

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(16)]
        [TestCase(100)]
        [TestCase(1024 - 1)]
        [TestCase(1024)]
        [TestCase(1151)]
        [TestCase(5345)]
        [TestCase(64564)]
        [TestCase(1024 * 1024 - 1)]
        [TestCase(1024 * 1024)]
        public void Should_always_return_a_buffer_of_at_least_given_minimum_size(int size)
        {
            using (pool.Rent(size, out var buffer))
            {
                buffer.Length.Should().BeGreaterOrEqualTo(size);
            }
        }

        [Test]
        public void Should_reuse_buffers()
        {
            var buffer = pool.Rent(123);

            for (var i = 0; i < 10; i++)
            {
                pool.Return(buffer);

                pool.Rent(123).Should().BeSameAs(buffer);
            }
        }

        [Test]
        public void Should_calculate_rented()
        {
            var initial = BufferPool.Rented;
            
            for (var i = 0; i < 10; i++)
            {
                var buffer = pool.Rent(123);

                BufferPool.Rented.Should().Be(initial + buffer.Length);

                pool.Return(buffer);

                BufferPool.Rented.Should().Be(initial);
            }
        }

        [Test]
        public void Should_allocate_new_buffers_as_needed()
        {
            var buffer1 = pool.Rent(123);
            var buffer2 = pool.Rent(123);
            var buffer3 = pool.Rent(123);

            buffer2.Should().NotBeSameAs(buffer1);
            buffer3.Should().NotBeSameAs(buffer1);
            buffer3.Should().NotBeSameAs(buffer2);
        }

        [Test]
        public void Should_allocate_arrays_past_max_stored_size()
        {
            for (var i = 0; i < 50; i++)
                pool.Rent(123).Length.Should().BeGreaterOrEqualTo(123);
        }
    }
}