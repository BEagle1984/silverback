// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Collections;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Messaging.Producing.Filter;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The producer configuration.
/// </summary>
public abstract record ProducerEndpointConfiguration : EndpointConfiguration
{
    private readonly IProducerEndpointResolver _endpointResolver = NullProducerEndpointResolver.Instance;

    /// <summary>
    ///     Gets the <see cref="IProducerEndpointResolver" /> to be used to resolve the destination endpoint (e.g. the target topic and
    ///     partition) for the message being produced.
    /// </summary>
    public IProducerEndpointResolver EndpointResolver
    {
        get => _endpointResolver;
        init
        {
            _endpointResolver = value;

            if (_endpointResolver != null)
                RawName = _endpointResolver.RawName;
        }
    }

    /// <summary>
    ///     Gets the <see cref="IMessageSerializer" /> to be used to serialize the messages being produced.
    ///     The default is the <see cref="JsonMessageSerializer" />.
    /// </summary>
    public IMessageSerializer Serializer { get; init; } = DefaultSerializers.Json;

    /// <summary>
    ///     Gets the message chunking settings. This option can be used to split large messages into smaller chunks.
    ///     The default is <c>null</c>, which means that chunking is disabled.
    /// </summary>
    public ChunkSettings? Chunk { get; init; }

    /// <summary>
    ///     Gets the strategy to be used to produce the messages.
    ///     The default is the <see cref="DefaultProduceStrategy" />.
    /// </summary>
    public IProduceStrategy Strategy { get; init; } = new DefaultProduceStrategy();

    /// <summary>
    ///     Gets the collection of <see cref="IOutboundMessageEnricher" /> to be used to enrich the outbound message.
    /// </summary>
    public IValueReadOnlyCollection<IOutboundMessageEnricher> MessageEnrichers { get; init; } =
        ValueReadOnlyCollection.Empty<IOutboundMessageEnricher>();

    /// <summary>
    ///     Gets the encryption settings to be used to encrypt the messages. The default is <c>null</c>, which means that the messages are
    ///     being sent in clear-text.
    /// </summary>
    public IEncryptionSettings? Encryption { get; init; }

    /// <summary>
    ///     Gets the <see cref="IOutboundMessageFilter" /> to be used to filter out messages that should not be produced.
    /// </summary>
    public IOutboundMessageFilter? Filter { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the produced messages can be subscribed to.
    ///     The default is <c>false</c>.
    /// </summary>
    public bool EnableSubscribing { get; init; }

    /// <summary>
    ///     Gets the (base) type of the messages being routed via this producer.
    /// </summary>
    internal Type MessageType { get; init; } = typeof(object);

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (EndpointResolver == null || Equals(EndpointResolver, NullProducerEndpointResolver.Instance))
        {
            throw new BrokerConfigurationException(
                "An endpoint resolver is required. " +
                $"Set the {nameof(EndpointResolver)} property or use ProduceTo or UseEndpointResolver to set it.");
        }

        if (Serializer == null)
            throw new BrokerConfigurationException("A serializer is required.");

        if (MessageType == null)
            throw new BrokerConfigurationException("The message type is required.");

        if (Strategy == null)
            throw new BrokerConfigurationException("A produce strategy is required.");

        if (Strategy is OutboxProduceStrategy && string.IsNullOrEmpty(FriendlyName))
            throw new BrokerConfigurationException("A unique friendly name for the endpoint is required when using the outbox produce strategy.");

        Chunk?.Validate();
        Encryption?.Validate();
    }
}
