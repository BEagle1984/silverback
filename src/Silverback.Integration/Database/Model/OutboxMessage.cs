// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Database.Model
{
    /// <summary>
    ///     The entity stored in the outbox table.
    /// </summary>
    public class OutboxMessage
    {
        /// <summary>
        ///     Gets or sets the primary key (identity).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        ///     Gets or sets the assembly qualified name of the message class.
        /// </summary>
        public string? MessageType { get; set; }

        /// <summary>
        ///     Gets or sets the message content (body).
        /// </summary>
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[]? Content { get; set; }

        /// <summary>
        ///     Gets or sets the serialized message headers.
        /// </summary>
        /// <remarks>
        ///     This field is no longer used (replaced by SerializedHeaders) and will be removed
        ///     with the next major release.
        /// </remarks>
        [Obsolete("Replaced by SerializedHeaders.")]
        public string? Headers { get; set; }

        /// <summary>
        ///     Gets or sets the serialized message headers.
        /// </summary>
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        public byte[]? SerializedHeaders { get; set; }

        /// <summary>
        ///     Gets or sets the name of the producer endpoint.
        /// </summary>
        [MaxLength(300)]
        public string EndpointName { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the name of the actual target endpoint that was resolved for the message.
        /// </summary>
        [MaxLength(300)]
        public string? ActualEndpointName { get; set; }

        /// <summary>
        ///     Gets or sets the datetime when the message was stored in the queue.
        /// </summary>
        public DateTime Created { get; set; }
    }
}
