// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     Store the chunks of the partially received message until all chunks are received and the the full
    ///     message can be rebuilt.
    /// </summary>
    public interface IChunkStore : ITransactional
    {
        /// <summary>
        ///     Gets a value indicating whether this <see cref="IChunkStore" /> holds some chunks in memory that
        ///     haven't been persisted to a permanent storage (or are about to be persisted at the next commit).
        ///     This information is used to decide whether the offsets can be committed.
        /// </summary>
        bool HasNotPersistedChunks { get; }

        /// <summary>
        ///     Stores the specified chunk.
        /// </summary>
        /// <param name="messageId">
        ///     The unique identifier of the message this chunk belongs to.
        /// </param>
        /// <param name="chunkIndex">
        ///     The sequential index of the chunk (within the message).
        /// </param>
        /// <param name="chunksCount">
        ///     The total number of chunks for this same message.
        /// </param>
        /// <param name="content">
        ///     The actual chunk binary content.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Store(string messageId, int chunkIndex, int chunksCount, byte[] content);

        /// <summary>
        ///     Returns the number of (unique) chunks stored for a certain message.
        /// </summary>
        /// <param name="messageId">
        ///     The message unique identifier.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the count.
        /// </returns>
        Task<int> CountChunks(string messageId);

        /// <summary>
        ///     Returns all stored chunks of the specified message.
        /// </summary>
        /// <param name="messageId">
        ///     The message unique identifier.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains a dictionary
        ///     with the chunks.
        /// </returns>
        Task<Dictionary<int, byte[]>> GetChunks(string messageId);

        /// <summary>
        ///     Removes all chunks related to the specified message. This method is called when the full message is
        ///     consumed. The changes will not be effective until <c>Commit</c> is called.
        /// </summary>
        /// <param name="messageId">
        ///     The message unique identifier.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Cleanup(string messageId);

        /// <summary>
        ///     Removes all chunks older than the specified threshold. The changes will be immediately effective
        ///     without having to invoke the <c>Commit</c> method.
        /// </summary>
        /// <param name="threshold">
        ///     The datetime specifying the max age of the chunks to be kept.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Cleanup(DateTime threshold);
    }
}
