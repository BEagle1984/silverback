// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Chunking
{
    /// <summary>
    ///     Used to periodically clean the <see cref="IChunkStore" />.
    /// </summary>
    public interface IChunkStoreCleaner
    {
        /// <summary>
        ///     Removes the older chunks according to the defined retention policy.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Cleanup();
    }
}
