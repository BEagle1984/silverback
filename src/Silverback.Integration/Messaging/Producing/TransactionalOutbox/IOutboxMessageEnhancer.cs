// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Enhances the outbound messages read from the outbox before they are produced. This can be used to add broker-specific information to
///     the message (e.g., Kafka key, MQTT correlation data, etc.).
/// </summary>
public interface IOutboxMessageEnhancer
{
    /// <summary>
    ///     Gets the type of the producer that this enhancer is associated with (identified by the related <see cref="ProducerEndpointConfiguration" />.
    ///     The enhancer will be applied only to the messages that are produced by a producer of this type.
    /// </summary>
    Type ProducerEndpointConfigurationType { get; }

    byte[]? GetExtra(IOutboundEnvelope envelope);

    /// <summary>
    ///     Enhances the outbound message.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope that will be produced. The enhancer can modify the envelope to add broker-specific information to the message.
    /// </param>
    /// <param name="outboxMessage">
    ///     The message read from the outbox.
    /// </param>
    void Enhance(IOutboundEnvelope envelope, OutboxMessage outboxMessage);
}

internal abstract class OutboxMessageEnhancer<TEndpointConfiguration> : IOutboxMessageEnhancer
{
    public Type ProducerEndpointConfigurationType { get; } = typeof(TEndpointConfiguration);

    public abstract byte[]? GetExtra(IOutboundEnvelope envelope);

    public abstract void Enhance(IOutboundEnvelope envelope, OutboxMessage outboxMessage);
}

public interface IOutboxMessageEnhancers
{
    IOutboxMessageEnhancer? GetEnhancer(ProducerEndpointConfiguration producerEndpointConfiguration);
}

public class OutboxMessageEnhancers : IOutboxMessageEnhancers
{
    private readonly Dictionary<Type, IOutboxMessageEnhancer> _enhancers;

    public OutboxMessageEnhancers(IEnumerable<IOutboxMessageEnhancer> enhancers)
    {
        _enhancers = Check.NotNull(enhancers, nameof(enhancers)).ToDictionary(
            enhancer => enhancer.ProducerEndpointConfigurationType,
            enhancer => enhancer);
    }

    public IOutboxMessageEnhancer? GetEnhancer(ProducerEndpointConfiguration producerEndpointConfiguration) =>
        _enhancers.GetValueOrDefault(Check.NotNull(producerEndpointConfiguration, nameof(producerEndpointConfiguration)).GetType());
}
