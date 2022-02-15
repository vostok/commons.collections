using System.Collections.Generic;
using System.Linq;

namespace Vostok.Commons.Collections
{
    internal static class SetExtensions
    {
#if NETSTANDARD2_0
        public static SetDiff<T> GetDiff<T>(this ISet<T> left, ISet<T> right)
#else
        public static SetDiff<T> GetDiff<T>(this IReadOnlySet<T> left, IReadOnlySet<T> right)
#endif
            where T : notnull
        {
            var inLeft = new HashSet<T>(left.Where(i => !right.Contains(i)));
            var same = new HashSet<T>(left.Where(right.Contains));
            var inRight = new HashSet<T>(right.Where(i => !left.Contains(i)));
            
            return new(inLeft, same, inRight);
        }
    }
}