// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Database.Model
{
    /// <summary>
    ///     The entity stored in the outbound queue table.
    /// </summary>
    public class OutboundMessage
    {
        /// <summary> Gets or sets the primary key (identity). </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary> Gets or sets the message content (body). </summary>
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Content { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the serialized message headers.
        /// </summary>
        public string Headers { get; set; } = null!;

        /// <summary> Gets or sets the serialized endpoint. </summary>
        public string Endpoint { get; set; } = null!;

        /// <summary> Gets or sets the endpoint name. </summary>
        [MaxLength(300)]
        public string EndpointName { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the datetime when the message was stored in the queue.
        /// </summary>
        public DateTime Created { get; set; }
    }
}
