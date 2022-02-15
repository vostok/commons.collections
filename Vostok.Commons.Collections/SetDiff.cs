using System.Collections.Generic;
using System.Linq;

namespace Vostok.Commons.Collections
{
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