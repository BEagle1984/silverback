// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    ///     The entity stored in the inbound log table.
    /// </summary>
    public class InboundMessage
    {
        /// <summary>
        ///     Gets or sets the unique identifier of the inbound message.
        /// </summary>
        [Key]
        [MaxLength(300)]
        public string MessageId { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the unique consumer group name of the consumer that received and processed the message.
        /// </summary>
        /// <remarks>
        ///     The column name is still <i> EndpointName </i> for backward compatibility but it now contains the
        ///     unique consumer group name retrieved from the <see cref="IConsumerEndpoint" />.
        /// </remarks>
        [Key]
        [MaxLength(300)]
        public string ConsumerGroupName { get; set; } = null!;

        /// <summary>
        ///     Gets or sets the datetime when the message was consumed.
        /// </summary>
        public DateTime Consumed { get; set; }
    }
}
