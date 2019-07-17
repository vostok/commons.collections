using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using Vostok.Commons.Collections.Tests.Implementations;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    [Explicit]
    public class ImmutableArrayDictionary_Benchmarks
    {
        [Test]
        public void WorstCase() => BenchmarkRunner.Run<HashCodeBenchmark_WorstCase>();

        [Test]
        public void Random() => BenchmarkRunner.Run<HashCodeBenchmark_Random>();
    }

    [MemoryDiagnoser]
    public abstract class HashCodeBenchmark
    {
        private readonly int itemCount;
        private readonly Func<int, string> getElement;
        private readonly string key;
        private ImmutableArrayDictionary_WithoutHashCode<string, string> withoutHash = new ImmutableArrayDictionary_WithoutHashCode<string, string>(StringComparer.Ordinal);
        private ImmutableArrayDictionary<string, string> withHash = new ImmutableArrayDictionary<string, string>(StringComparer.Ordinal);

        protected HashCodeBenchmark(int itemCount, string key, Func<int, string> getElement)
        {
            this.itemCount = itemCount;
            this.getElement = getElement;
            this.key = key;
        }

        [GlobalSetup]
        public void Setup()
        {
            for (var i = 0; i < itemCount; ++i)
            {
                var element = getElement(i);
                withHash = withHash.Set(element, "");
                withoutHash = withoutHash.Set(element, "");
            }
        }

        [Benchmark]
        public void WithoutHash() => withoutHash.TryGetValue(key, out _);

        [Benchmark]
        public void WithHash() => withHash.TryGetValue(key, out _);
    }

    public class HashCodeBenchmark_WorstCase : HashCodeBenchmark
    {
        public HashCodeBenchmark_WorstCase()
            : base(100, new string('a', 100) + 99, i => new string('a', 100) + i)
        {
        }
    }

    public class HashCodeBenchmark_Random : HashCodeBenchmark
    {
        public HashCodeBenchmark_Random()
            : base(100, Guid.Empty.ToString(), _ => Guid.NewGuid().ToString())
        {
        }
    }
}