﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;

namespace Vostok.Commons.Collections
{
    [PublicAPI]
    internal class BufferPool
    {
        private const int DefaultMaximumArraySize = 1024 * 1024;
        private const int DefaultMaximumArraysPerBucket = 20;

        private const int MinimumArrayLength = 0x10;
        private const int MaximumArrayLength = 0x40000000;
        private const int MaximumBucketsToTry = 2;

        public static BufferPool Default = new BufferPool();

        private static long rentedTotal;

        private readonly Bucket[] buckets;
        private long rentedFromInstance;


        public BufferPool(
            int maxArraySize = DefaultMaximumArraySize,
            int maxArraysPerBucket = DefaultMaximumArraysPerBucket)
        {
            if (maxArraySize < MinimumArrayLength)
                maxArraySize = MinimumArrayLength;

            if (maxArraySize > MaximumArrayLength)
                maxArraySize = MaximumArrayLength;

            buckets = new Bucket[SelectBucketIndex(maxArraySize) + 1];

            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new Bucket(GetMaxSizeForBucket(i), maxArraysPerBucket);
            }
        }

        public static long Rented => Interlocked.Read(ref rentedTotal);
        public long RentedFromInstance => Interlocked.Read(ref rentedFromInstance);

        [NotNull]
        public IDisposable Rent(int minimumSize, out byte[] buffer)
        {
            buffer = Rent(minimumSize);
            return new RentToken(this, buffer);
        }

        [NotNull]
        public IDisposable RentWithInfo(int minimumSize, out byte[] buffer, out bool fromBucket)
        {
            buffer = RentWithInfo(minimumSize, out fromBucket);
            return new RentToken(this, buffer);
        }

        public byte[] Rent(int minimumSize)
        {
            var buffer = RentInternal(minimumSize).instance;
            UpdateRented(buffer.Length);
            return buffer;
        }

        public byte[] RentWithInfo(int minimumSize, out bool fromBucket)
        {
            var buffer = RentInternal(minimumSize);
            fromBucket = buffer.fromBucket;
            UpdateRented(buffer.instance.Length);
            return buffer.instance;
        }

        public void Return(byte[] buffer, bool clear = false)
        {
            if (buffer == null || buffer.Length == 0)
                return;

            var bucket = SelectBucketIndex(buffer.Length);
            if (bucket < buckets.Length)
            {
                if (clear)
                    Array.Clear(buffer, 0, buffer.Length);

                if (buckets[bucket].Return(buffer))
                    UpdateRented(-buffer.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SelectBucketIndex(int bufferSize)
        {
            var bitsRemaining = ((uint)bufferSize - 1) >> 4;

            var poolIndex = 0;
            if (bitsRemaining > 0xFFFF)
            {
                bitsRemaining >>= 16;
                poolIndex = 16;
            }

            if (bitsRemaining > 0xFF)
            {
                bitsRemaining >>= 8;
                poolIndex += 8;
            }

            if (bitsRemaining > 0xF)
            {
                bitsRemaining >>= 4;
                poolIndex += 4;
            }

            if (bitsRemaining > 0x3)
            {
                bitsRemaining >>= 2;
                poolIndex += 2;
            }

            if (bitsRemaining > 0x1)
            {
                bitsRemaining >>= 1;
                poolIndex += 1;
            }

            return poolIndex + (int)bitsRemaining;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMaxSizeForBucket(int binIndex)
            => 16 << binIndex;

        private void UpdateRented(int len)
        {
            Interlocked.Add(ref rentedTotal, len);
            Interlocked.Add(ref rentedFromInstance, len);
        }

        private (byte[] instance, bool fromBucket) RentInternal(int minimumSize)
        {
            if (minimumSize < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumSize));

            if (minimumSize == 0)
                return (Array.Empty<byte>(), true);

            var index = SelectBucketIndex(minimumSize);
            if (index < buckets.Length)
            {
                var localIndex = index;
                do
                {
                    var buffer = buckets[localIndex].Rent();
                    if (buffer != null)
                        return (buffer, true);
                } while (++localIndex < buckets.Length && localIndex != index + MaximumBucketsToTry);

                return (new byte[buckets[index].BufferSize], false);
            }

            return (new byte[minimumSize], false);
        }

        private class RentToken : IDisposable
        {
            private readonly byte[] buffer;
            private volatile BufferPool pool;

            public RentToken(BufferPool pool, byte[] buffer)
            {
                this.pool = pool;
                this.buffer = buffer;
            }

            public void Dispose()
                => Interlocked.Exchange(ref pool, null)?.Return(buffer);
        }

        private class Bucket
        {
            private readonly byte[][] buffers;
            private SpinLock sync;
            private int index;

            public Bucket(int bufferSize, int buffersCount)
            {
                BufferSize = bufferSize;

                buffers = new byte[buffersCount][];

                sync = new SpinLock(false);
            }

            public int BufferSize { get; }

            public byte[] Rent()
            {
                byte[] buffer = null;

                var lockTaken = false;
                var allocateBuffer = false;

                try
                {
                    sync.Enter(ref lockTaken);

                    if (index < buffers.Length)
                    {
                        buffer = buffers[index];
                        buffers[index++] = null;
                        allocateBuffer = buffer == null;
                    }
                }
                finally
                {
                    if (lockTaken)
                        sync.Exit(false);
                }

                if (allocateBuffer)
                    buffer = new byte[BufferSize];

                return buffer;
            }

            public bool Return(byte[] array)
            {
                if (array.Length != BufferSize)
                    throw new ArgumentException($"Attempt to return buffer of size {array.Length} to a bucket with size {BufferSize}.");

                var lockTaken = false;

                try
                {
                    sync.Enter(ref lockTaken);

                    if (index != 0)
                    {
                        buffers[--index] = array;
                        return true;
                    }
                }
                finally
                {
                    if (lockTaken)
                        sync.Exit(false);
                }

                return false;
            }
        }
    }
}