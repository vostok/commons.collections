using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Vostok.Commons.Collections.Tests
{
    [TestFixture]
    [Explicit]
    public class ConcurrentBoundedQueue_Tests_Smoke
    {
        private ConcurrentBoundedQueue<MyClass> testedQueue;
        private ConcurrentQueue<MyClass> referenceQueue;
        private StringBuilder readerInfo;
        private List<int> bufferReads;
        private List<int> queueReads;
        private int writerDone;
        private bool readerDone;

        [Test, Repeat(20)]
        public void Writers_should_write_all_data_reader_should_read_all_data()
        {
            testedQueue = new ConcurrentBoundedQueue<MyClass>(1000);
            referenceQueue = new ConcurrentQueue<MyClass>();
            readerInfo = new StringBuilder();
            bufferReads = new List<int>();
            queueReads = new List<int>();
            writerDone = 0;
            readerDone = false;

            const int taskCount = 100;

            Console.WriteLine($"{DateTime.Now} Start");
            Task.Run(() => Reader());
            for (var i = 0; i < taskCount; i++)
                Task.Run(() => Writer());

            while (writerDone != taskCount)
                Thread.Sleep(300.Milliseconds());
            Console.WriteLine($"{DateTime.Now} Writers are done");
            while (!readerDone)
                Thread.Sleep(300.Milliseconds());
            Thread.Sleep(500.Milliseconds());
            // Console.WriteLine(readerInfo.ToString());
            Console.WriteLine($"{DateTime.Now} Reader is done");

            Console.WriteLine($"Buffer: {testedQueue.Count}");
            Console.WriteLine($"Queue: {referenceQueue.Count}");

            bufferReads.Should().Equal(queueReads);
            bufferReads.Sum()
                .Should()
                .Be(queueReads.Sum())
                .And.Be(taskCount * 3000 /*Writer().counter*/);
            testedQueue.Count
                .Should()
                .Be(referenceQueue.Count)
                .And.Be(0);
        }

        private void Writer()
        {
            const int counter = 3000;
            for (var i = 0; i < counter; i++)
            {
                var val = new MyClass();
                if (testedQueue.TryAdd(val))
                    referenceQueue.Enqueue(val);
                else i--;
            }

            Interlocked.Increment(ref writerDone);
        }

        private void Reader()
        {
            const int size = 50;
            var buffer = new MyClass[size];

            var zeros = 0;
            var tries = 0;
            while (true)
            {
                var bbCnt = testedQueue.Drain(buffer, 0, size);
                var qCnt = 0;
                for (var i = 0; i < bbCnt; i++)
                {
                    if (referenceQueue.TryDequeue(out _))
                        qCnt++;
                    else i--;
                }

                /*if (bbCnt == 0 && (bufferReads.Count == 0 || zeros > 1000 && boundedBuffer.Count > 0))
                {
                    tries++;
                    Thread.Sleep(1.Milliseconds());
                    continue;
                }*/

                bufferReads.Add(bbCnt);
                queueReads.Add(qCnt);
                if (tries > 0)
                {
                    readerInfo.AppendLine($"Tried to reed: {tries}");
                    tries = 0;
                }
                if (zeros < 10)
                    readerInfo.AppendLine($"{DateTime.Now} Bb: {bbCnt}, Q: {qCnt}");

                if (bbCnt == 0 && qCnt == 0)
                    zeros++;
                else
                {
                    if (zeros > 300)
                        readerInfo.AppendLine($"Zeros: {zeros}");
                    zeros = 0;
                }
                if (zeros == 1_000_000)
                    break;
            }

            readerDone = true;
        }

        private class MyClass
        {
        }
    }
}