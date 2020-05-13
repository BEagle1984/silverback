// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Database.Model
{
    /// <summary>
    ///     The entity stored in the inbound log table.
    /// </summary>
    public class InboundLogEntry
    {
        /// <summary>
        ///     Gets or sets the unique identifier of the inbound message.
        /// </summary>
        [Key]
        [MaxLength(300)]
        public string MessageId { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the name of the endpoint the message was consumed from.
        /// </summary>
        [Key]
        [MaxLength(300)]
        public string EndpointName { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the unique consumer group name of the consumer that received and processed the message.
        /// </summary>
        [Key]
        [MaxLength(300)]
        public string ConsumerGroupName { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the datetime when the message was consumed.
        /// </summary>
        public DateTime Consumed { get; set; }
    }
}
