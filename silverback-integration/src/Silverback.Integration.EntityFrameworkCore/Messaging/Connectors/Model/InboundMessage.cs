// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    /// The entity to be stored in the inbound log table.
    /// </summary>

    public class InboundMessage
    {
        [Key]
        public Guid MessageId { get; set; }

        [Key]
        public string EndpointName { get; set; }

        public string Message { get; set; }

        public DateTime Consumed { get; set; }
    }
}