// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     The factory used to build the <see cref="IOutboundEnvelope" /> or <see cref="IOutboundEnvelope{TMessage}" /> instances.
/// </summary>
public interface IOutboundEnvelopeFactory
{
    /// <summary>
    ///     Creates a new instance of <see cref="IOutboundEnvelope" /> or <see cref="IOutboundEnvelope{TMessage}" />.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped in the envelope.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The destination endpoint.
    /// </param>
    /// <param name="producer">
    ///     The producer to be used to produce this message.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> instance.
    /// </returns>
    IOutboundEnvelope CreateEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer);
}
