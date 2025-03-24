// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Returns an <see cref="IProducer" /> that can be used to produce messages against the message broker.
/// </summary>
public interface IProducerCollection : IReadOnlyCollection<IProducer>
{
    /// <summary>
    ///     Gets a producer for the specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    IProducer GetProducerForEndpoint(string endpointName);

    /// <summary>
    ///     Gets the producers that are compatible with the specified message type (and are configured for routing).
    /// </summary>
    /// <param name="messageType">
    ///     The message type.
    /// </param>
    /// <returns>
    ///     A collection of <see cref="IProducer" /> to be used to produce the message.
    /// </returns>
    IReadOnlyCollection<IProducer> GetProducersForMessage(Type messageType);
}
