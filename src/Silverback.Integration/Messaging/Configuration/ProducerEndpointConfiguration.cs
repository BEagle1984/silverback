// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Collections;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Producing;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The producer configuration.
/// </summary>
public abstract record ProducerEndpointConfiguration : EndpointConfiguration
{
    private readonly IProducerEndpointResolver _endpoint = NullProducerEndpointResolver.Instance;

    /// <summary>
    ///     Gets the <see cref="IProducerEndpointResolver" /> to be used to resolve the target endpoint (e.g. the target topic and
    ///     partition) for the message being produced.
    /// </summary>
    public IProducerEndpointResolver Endpoint
    {
        get => _endpoint;
        init
        {
            _endpoint = value;

            if (_endpoint != null)
                RawName = _endpoint.RawName;
        }
    }

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
    ///     Gets the (base) type of the messages being routed via this producer.
    /// </summary>
    internal Type MessageType { get; init; } = typeof(object);

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Endpoint == null || Equals(Endpoint, NullProducerEndpointResolver.Instance))
        {
            throw new BrokerConfigurationException(
                "An endpoint resolver is required. " +
                $"Set the {nameof(Endpoint)} property or use ProduceTo or UseEndpointResolver to set it.");
        }

        if (MessageType == null)
            throw new BrokerConfigurationException("The message type is required.");

        if (Strategy == null)
            throw new BrokerConfigurationException("A produce strategy is required.");

        Chunk?.Validate();
        Encryption?.Validate();
    }
}
