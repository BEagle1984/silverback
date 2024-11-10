// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message that is being produced to an outbound endpoint.
/// </summary>

// TODO: Merge IRawOutboundEnvelope and IOutboundEnvelope (same for related classes)
public interface IRawOutboundEnvelope : IRawBrokerEnvelope
{
    /// <summary>
    ///     Gets the destination endpoint configuration.
    /// </summary>
    ProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <summary>
    ///     Gets the <see cref="IProducer" /> that will be or was used to produce this message.
    /// </summary>
    IProducer Producer { get; }

    /// <summary>
    ///     Gets the message identifier on the message broker (the Kafka offset or similar).
    /// </summary>
    /// <remarks>
    ///     The identifier value will be set only after the message has been successfully published to the message
    ///     broker.
    /// </remarks>
    IBrokerMessageIdentifier? BrokerMessageIdentifier { get; }

    /// <summary>
    ///     Gets the current <see cref="ISilverbackContext" />.
    /// </summary>
    ISilverbackContext? Context { get; }

    /// <summary>
    ///     Adds a new header.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IRawOutboundEnvelope AddHeader(string name, object value);

    /// <summary>
    ///     Adds a new header or replaces the header with the same name.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IRawOutboundEnvelope AddOrReplaceHeader(string name, object? newValue);

    /// <summary>
    ///     Adds a new header if no header with the same name is already set.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="newValue">
    ///     The new header value.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IRawOutboundEnvelope AddHeaderIfNotExists(string name, object? newValue);

    /// <summary>
    ///     Sets the message id header (<see cref="DefaultMessageHeaders.MessageId" />).
    /// </summary>
    /// <param name="value">
    ///     The message id.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IRawOutboundEnvelope SetMessageId(object? value);
}
