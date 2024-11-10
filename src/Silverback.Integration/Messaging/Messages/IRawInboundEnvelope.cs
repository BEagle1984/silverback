// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message that is being consumed from an inbound endpoint.
/// </summary>
public interface IRawInboundEnvelope : IRawBrokerEnvelope
{
    /// <summary>
    ///     Gets the source endpoint. It is mostly relevant when the <see cref="EndpointConfiguration" /> points to multiple endpoints
    ///     (for example if consuming multiple topics with a single consumer).
    /// </summary>
    ConsumerEndpoint Endpoint { get; }

    /// <summary>
    ///     Gets the <see cref="IConsumer" /> that consumed this message.
    /// </summary>
    IConsumer Consumer { get; }

    /// <summary>
    ///     Gets the message identifier on the message broker (the Kafka offset or similar).
    /// </summary>
    IBrokerMessageIdentifier BrokerMessageIdentifier { get; }
}
