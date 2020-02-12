// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    ///     The entity to be stored in the inbound log table.
    /// </summary>
    public class InboundMessage
    {
        [Key, MaxLength(300)]
        public string MessageId { get; set; }

        /// <summary>
        ///     Note: the name was maintained for backward compatibility but it now contains the
        ///     <see cref="IConsumerEndpoint" />'s unique consumer group name.
        /// </summary>
        [Key, MaxLength(300)]
        public string EndpointName { get; set; }

        public string Message { get; set; }

        public DateTime Consumed { get; set; }
    }
}