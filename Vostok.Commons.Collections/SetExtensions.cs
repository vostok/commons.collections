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
    
    internal class SetDiff<T>
    {
#if !NETSTANDARD2_0
        public IReadOnlySet<T> OnlyInLeft { get; }
        public IReadOnlySet<T> Same { get; }
        public IReadOnlySet<T> OnlyInRight { get; }
#else
        public IReadOnlyCollection<T> OnlyInLeft { get; }
        public IReadOnlyCollection<T> Same { get; }
        public IReadOnlyCollection<T> OnlyInRight { get; }
#endif

        public IEnumerable<T> Different => OnlyInLeft.Concat(OnlyInRight);

#if !NETSTANDARD2_0
        public SetDiff(
            IReadOnlySet<T> onlyInLeft,
            IReadOnlySet<T> same,
            IReadOnlySet<T> onlyInRight)
        {
            OnlyInLeft = onlyInLeft;
            Same = same;
            OnlyInRight = onlyInRight;
        }
#else
        public SetDiff(
            IReadOnlyCollection<T> onlyInLeft,
            IReadOnlyCollection<T> same,
            IReadOnlyCollection<T> onlyInRight)
        {
            OnlyInLeft = onlyInLeft;
            Same = same;
            OnlyInRight = onlyInRight;
        }
#endif
    }
}