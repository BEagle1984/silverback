// Copyright (c) 2018-2019 Sergio Aquilini
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
        [Key, MaxLength(300)]
        public string MessageId { get; set; }

        public string Message { get; set; }

        [MaxLength(300)]
        public string EndpointName { get; set; }

        public string Endpoint { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Produced { get; set; }
    }
}