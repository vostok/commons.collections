using System;

namespace Vostok.Commons.Collections
{
    internal static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random random;

        public static int Next(int maxValue)
        {
            return ObtainRandom().Next(maxValue);
        }

        public static int Next(int minValue, int maxValue)
        {
            return ObtainRandom().Next(minValue, maxValue);
        }

        private static Random ObtainRandom()
        {
            return random ?? (random = new Random(Guid.NewGuid().GetHashCode()));
        }
    }
}