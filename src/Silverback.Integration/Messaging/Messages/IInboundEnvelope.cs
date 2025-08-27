// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the consumed message.
/// </summary>
public interface IInboundEnvelope : IBrokerEnvelope
{
    /// <summary>
    ///     Gets the <see cref="ConsumerEndpoint" /> from which the message was consumed.
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

    /// <summary>
    ///     Clones the envelope and replaces the raw message with the specified one.
    /// </summary>
    /// <param name="newRawMessage">
    ///     The new raw message to be set.
    /// </param>
    /// <returns>
    ///     The new envelope.
    /// </returns>
    IInboundEnvelope CloneReplacingRawMessage(Stream? newRawMessage);
}
