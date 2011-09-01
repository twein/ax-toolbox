using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Scorer
{
    public static class CollectionExtensions
    {
        public static Collection<TSource> Sort<TSource, TKey>(this Collection<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> sortedList = source.OrderBy(keySelector).ToList();
            source.Clear();
            foreach (var sortedItem in sortedList)
                source.Add(sortedItem);

            return source;
        }

        public static void CopyTo<T>(this Collection<T> source, Collection<T> dest)
        {
            dest.Clear();

            foreach (var item in source)
                dest.Add(item);
        }
    }
}
