// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Collections;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging;

/// <summary>
///     The producer configuration.
/// </summary>
public abstract record ProducerConfiguration : EndpointConfiguration
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

    /// <inheritdoc cref="EndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Endpoint == null || Endpoint == NullProducerEndpointResolver.Instance)
        {
            throw new EndpointConfigurationException(
                    "An endpoint resolver is required. " +
                    $"Set the {nameof(Endpoint)} property or use ProduceTo or UseEndpointResolver to set it.",
                    Endpoint,
                    nameof(Endpoint));
        }

        if (Strategy == null)
            throw new EndpointConfigurationException("A produce strategy is required.", Strategy, nameof(Strategy));

        Chunk?.Validate();
    }
}
