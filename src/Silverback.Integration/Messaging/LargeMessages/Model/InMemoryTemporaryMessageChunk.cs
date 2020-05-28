// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.LargeMessages.Model
{
    /// <summary>
    ///     The temporary chunk stored in memory.
    /// </summary>
    public class InMemoryTemporaryMessageChunk
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryTemporaryMessageChunk" /> class.
        /// </summary>
        /// <param name="messageId">
        ///     The identifier of the message that was chunked.
        /// </param>
        /// <param name="chunkIndex">
        ///     The chunk index (sequence).
        /// </param>
        /// <param name="content">
        ///     The chunk binary content.
        /// </param>
        public InMemoryTemporaryMessageChunk(string messageId, int chunkIndex, byte[] content)
        {
            MessageId = messageId;
            ChunkIndex = chunkIndex;
            Content = content;
        }

        /// <summary>
        ///     Gets the identifier of the message that was chunked.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        ///     Gets the chunk index (sequence).
        /// </summary>
        public int ChunkIndex { get; }

        /// <summary>
        ///     Gets the chunk binary content.
        /// </summary>
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[] Content { get; }
    }
}
