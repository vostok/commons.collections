using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture(true)]
    [TestFixture(false)]
    internal class CachingTransform_Tests
    {
        private readonly bool preventParallelProcessing;

        private CachingTransform<string, string> transform;

        private string providedValue;

        private int processorCalls;

        public CachingTransform_Tests(bool preventParallelProcessing)
        {
            this.preventParallelProcessing = preventParallelProcessing;
        }

        [SetUp]
        public void TestSetup()
        {
            providedValue = "xxx";

            processorCalls = 0;

            transform = new CachingTransform<string, string>(i =>
            {
                processorCalls++;
                return i.ToUpper();
            }, () => providedValue, preventParallelProcessing: preventParallelProcessing);
        }

        [Test]
        public void Get_without_parameters_should_work_when_there_is_no_cache()
        {
            transform.Get().Should().Be("XXX");
        }

        [Test]
        public void Get_without_parameters_should_return_cached_value_without_processing_when_raw_value_does_not_change()
        {
            var result1 = transform.Get();
            var result2 = transform.Get();
            var result3 = transform.Get();

            result2.Should().BeSameAs(result1);
            result3.Should().BeSameAs(result1);

            processorCalls.Should().Be(1);
        }

        [Test]
        public void Get_without_parameters_should_react_to_raw_value_change()
        {
            transform.Get().Should().Be("XXX");

            providedValue = "xxxx";

            transform.Get().Should().Be("XXXX");
        }

        [Test]
        public void Get_with_parameter_should_always_return_correct_transformed_value()
        {
            providedValue = "x";

            transform.Get("xx").Should().Be("XX");
            transform.Get();
            transform.Get("xxx").Should().Be("XXX");
            transform.Get();
            transform.Get("xxxx").Should().Be("XXXX");
        }
    }
}