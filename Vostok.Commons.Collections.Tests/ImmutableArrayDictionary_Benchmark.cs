using System;
using System.Collections.Generic;
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
        public void WorstCase_TryGet() => BenchmarkRunner.Run<HashCodeBenchmark_WorstCase>();
        
        [Test]
        public void Random_TryGet() => BenchmarkRunner.Run<HashCodeBenchmark_Random>();

        [Test]
        public void ImmutableArrayDictionary_SettersBenchmark_Small_Preallocate() => BenchmarkRunner.Run<ImmutableArrayDictionary_SettersBenchmark>();
    }

    [MemoryDiagnoser]
    public abstract class ImmutableArrayDictionary_SettersBenchmark
    {
        private (string key, string value)[] pairsToSet;
        private ImmutableArrayDictionary<string, string> dict;


        [Params(4, 25, 250)]
        public int Count;

        [Params(true, false)]
        public bool Preallocate;

        protected ImmutableArrayDictionary_SettersBenchmark()
        {
        }

        [GlobalSetup]
        public void Setup()
        {
            pairsToSet = PairsCreator.CreatePairs(Count);

            dict = Preallocate ? new ImmutableArrayDictionary<string, string>(pairsToSet.Length) : new ImmutableArrayDictionary<string, string>();
        }

        [Benchmark]
        public IReadOnlyDictionary<string, string> SetRangeWithArrayCreationImitation()
        {
            var pairsToAddCreatingImitation = new (string key, string value)[pairsToSet.Length];
            for (var i = 0; i < pairsToSet.Length; i++)
                pairsToAddCreatingImitation[i] = pairsToSet[i];
            return dict.SetRange(pairsToSet);
        }

        [Benchmark]
        public IReadOnlyDictionary<string, string> SetRange()
        {
            return dict.SetRange(pairsToSet);
        }

        [Benchmark]
        public IReadOnlyDictionary<string, string> Set()
        {
            var dictCopy = dict;
            for (var index = 0; index < pairsToSet.Length; index++)
            {
                var (key, value) = pairsToSet[index];
                dictCopy = dictCopy.Set(key, value);
            }

            return dictCopy;
        }
    }

    internal static class PairsCreator
    {
        public static (string key, string value)[] CreatePairs(int length)
        {
            var pairs = new (string key, string value)[length];
            for (var i = 0; i < pairs.Length; i++)
                pairs[i] = (Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            return pairs;
        }
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