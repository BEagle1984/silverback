using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Extensions
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Executes the specified action against all elements in the <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
            {
                action(element);
            }
        }

        /// <summary>
        /// Executes the specified asynchronous action against all elements in the <see cref="IEnumerable{T}"/>.
        /// The actions are executed sequentially.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            foreach (var element in source)
            {
                await action(element);
            }
        }
    }
}
