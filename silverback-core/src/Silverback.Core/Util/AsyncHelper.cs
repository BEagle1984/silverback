using System;
using System.Threading.Tasks;

namespace Silverback.Util
{
    /// <summary>
    /// Provides some methods to synchronously run an async method.
    /// </summary>
    internal static class AsyncHelper
    {
        /// <summary>
        /// Runs the async method synchronously (avoiding deadlocks).
        /// </summary>
        public static void RunSynchronously(Func<Task> asyncMethod)
            => Task.Run(asyncMethod).Wait();

        /// <summary>
        /// Runs the async method synchronously(avoiding deadlocks).
        /// </summary>
        public static TResult RunSynchronously<TResult>(Func<Task<TResult>> asyncMethod)
            => Task.Run(asyncMethod).Result;
    }
}
