using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegistryLanguageService
{
    internal static class Extensions
    {
        public static IEnumerable<T> SwitchToIfEmpty<T>(this IEnumerable<T> source, IEnumerable<T> second)
        {
            return source.Any()
                ? source
                : second
            ;
        }

        public static IEnumerable<T> TakeTo<T>(this IEnumerable<T> source, T subject)
        {
            foreach (T element in source)
            {
                yield return element;

                if (Object.Equals(element, subject))
                    yield break;
            }
        }

        public static bool ContainsOrEndsWith(this SnapshotSpan span, SnapshotPoint point)
        {
            return span.Contains(point) || span.End == point;
        }
    }
}
