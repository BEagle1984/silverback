// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message consumed from the message broker.
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

    IInboundEnvelope SetRawMessage(Stream? rawMessage);

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

internal interface IInternalInboundEnvelope : IInboundEnvelope
{
    /// <summary>
    ///    Gets the message key, if available (e.g. Kafka message key).
    /// </summary>
    /// <returns>
    ///     The message key.
    /// </returns>
    /// <remarks>
    ///     This method exists only to support the legacy <see cref="Tombstone" />.
    /// </remarks>
    string? GetKey();
}
