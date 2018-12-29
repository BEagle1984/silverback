// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    /// The entity to be stored in the outbound queue table.
    /// </summary>
    public class OutboundMessage
    {
        [Key]
        public Guid MessageId { get; set; }

        public string Message { get; set; }

        public string EndpointName { get; set; }

        public string Endpoint { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Produced { get; set; }
    }
}