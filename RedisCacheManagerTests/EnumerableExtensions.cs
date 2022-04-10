using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisCacheManagerTests
{
    internal static class EnumerableExtensions
    {
        internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var entry in source)
            {
                yield return entry;
            }

            await Task.CompletedTask;
        }

    }
}
