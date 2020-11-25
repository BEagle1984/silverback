// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     The message that is periodically produced by the <see cref="OutboundEndpointsHealthCheckService" />
    ///     to verify that the endpoints are reachable.
    /// </summary>
    public class PingMessage : IMessage
    {
        /// <summary>
        ///     Gets or sets the datetime at which the message has been produced.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        ///     Creates a new instance of the <see cref="PingMessage" />.
        /// </summary>
        /// <returns>
        ///     The new <see cref="PingMessage" />.
        /// </returns>
        public static PingMessage New() => new() { TimeStamp = DateTime.UtcNow };
    }
}
