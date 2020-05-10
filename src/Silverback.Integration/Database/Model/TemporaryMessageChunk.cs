// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Database.Model
{
    /// <summary>
    ///     The entity stored in the temporary chunk storage table.
    /// </summary>
    public class TemporaryMessageChunk
    {
        /// <summary>
        ///     Gets or sets the identifier of the message that was chunked.
        /// </summary>
        [Key]
        [MaxLength(300)]
        public string MessageId { get; set; } = null!;

        /// <summary> Gets or sets the chunk index (sequence). </summary>
        [Key]
        public int ChunkIndex { get; set; }

        /// <summary>
        ///     Gets or sets the total number of chunks that were produced for this same message.
        /// </summary>
        public int ChunksCount { get; set; }

        /// <summary> Gets or sets the chunk binary content. </summary>
        [SuppressMessage("ReSharper", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[] Content { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the datetime when the chunk was received.
        /// </summary>
        public DateTime Received { get; set; }
    }
}
