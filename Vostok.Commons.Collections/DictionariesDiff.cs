using System.Collections.Generic;
using System.Linq;

namespace Vostok.Commons.Collections
{
    internal class DictionariesDiff<TKey>
    {
#if !NETSTANDARD2_0
        public IReadOnlySet<TKey> OnlyInLeft { get; }
        public IReadOnlySet<TKey> Changed { get; }
        public IReadOnlySet<TKey> Same { get; }
        public IReadOnlySet<TKey> OnlyInRight { get; }
#else
        public IReadOnlyCollection<TKey> OnlyInLeft { get; }
        public IReadOnlyCollection<TKey> Changed { get; }
        public IReadOnlyCollection<TKey> Same { get; }
        public IReadOnlyCollection<TKey> OnlyInRight { get; }
#endif

        public IEnumerable<TKey> Different => OnlyInLeft.Concat(OnlyInRight).Concat(Changed);

#if !NETSTANDARD2_0
        public DictionariesDiff(
            IReadOnlySet<TKey> onlyInLeft,
            IReadOnlySet<TKey> changed,
            IReadOnlySet<TKey> same,
            IReadOnlySet<TKey> onlyInRight)
        {
            OnlyInLeft = onlyInLeft;
            Changed = changed;
            Same = same;
            OnlyInRight = onlyInRight;
        }
#else
        public DictionariesDiff(
            IReadOnlyCollection<TKey> onlyInLeft,
            IReadOnlyCollection<TKey> changed,
            IReadOnlyCollection<TKey> same,
            IReadOnlyCollection<TKey> onlyInRight)
        {
            OnlyInLeft = onlyInLeft;
            Changed = changed;
            Same = same;
            OnlyInRight = onlyInRight;
        }
#endif
    }
}