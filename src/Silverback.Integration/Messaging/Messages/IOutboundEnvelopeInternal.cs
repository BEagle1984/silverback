// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc />
    internal interface IOutboundEnvelopeInternal : IOutboundEnvelope
    {
        /// <summary>
        ///     Gets the type of the <see cref="IOutboundConnector" /> to be used when publishing these messages. If
        ///     <c>
        ///         null
        ///     </c>, the default <see cref="IOutboundConnector" /> will be used.
        /// </summary>
        Type? OutboundConnectorType { get; }
    }
}
