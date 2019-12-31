// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silverback.Messaging.Connectors.Model
{
    /// <summary>
    ///     The entity to be stored in the outbound queue table.
    /// </summary>
    public class OutboundMessage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public byte[] Content { get; set; }
        public string Headers { get; set; }
        public string Endpoint { get; set; }
        public string EndpointName { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Produced { get; set; }
    }
}