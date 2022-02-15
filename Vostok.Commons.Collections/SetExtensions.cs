using System.Collections.Generic;
using System.Linq;

namespace Vostok.Commons.Collections
{
    internal static class SetExtensions
    {
#if NET5 || NET6
        public static SetDiff<T> GetDiff<T>(this IReadOnlySet<T> left, IReadOnlySet<T> right)
#else
        public static SetDiff<T> GetDiff<T>(this ISet<T> left, ISet<T> right)
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
#if NET5 || NET6
        public IReadOnlySet<T> OnlyInLeft { get; }
        public IReadOnlySet<T> Same { get; }
        public IReadOnlySet<T> OnlyInRight { get; }
#else
        public IReadOnlyCollection<T> OnlyInLeft { get; }
        public IReadOnlyCollection<T> Same { get; }
        public IReadOnlyCollection<T> OnlyInRight { get; }
#endif

        public IEnumerable<T> Different => OnlyInLeft.Concat(OnlyInRight);

#if NET5 || NET6
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

        public void Deconstruct(
#if NET5 || NET6
            out IReadOnlySet<T> onlyInLeft,
            out IReadOnlySet<T> same,
            out IReadOnlySet<T> onlyInRight
#else
            out IReadOnlyCollection<T> onlyInLeft,
            out IReadOnlyCollection<T> same,
            out IReadOnlyCollection<T> onlyInRight
#endif
        )
        {
            onlyInLeft = OnlyInLeft;
            same = Same;
            onlyInRight = OnlyInRight;
        }
    }
}