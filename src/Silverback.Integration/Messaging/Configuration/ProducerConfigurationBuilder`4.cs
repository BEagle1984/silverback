// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Collections;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The base class for the builders of the types inheriting from <see cref="ConsumerConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being produced.
/// </typeparam>
/// <typeparam name="TConfiguration">
///     The type of the configuration being built.
/// </typeparam>
/// <typeparam name="TEndpoint">
///     The type of the endpoint.
/// </typeparam>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
[SuppressMessage("Design", "CA1005:Avoid excessive parameters on generic types", Justification = "Not instantiated directly")]
public abstract partial class ProducerConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
    : EndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    where TConfiguration : ProducerConfiguration<TEndpoint>
    where TEndpoint : ProducerEndpoint
    where TBuilder : ProducerConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    private readonly List<IOutboundMessageEnricher> _messageEnrichers = new();

    private IProduceStrategy? _strategy;

    private int? _chunkSize;

    private bool? _alwaysAddChunkHeaders;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducerConfigurationBuilder{TMessage, TConfiguration, TEndpoint, TBuilder}" /> class.
    /// </summary>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    protected ProducerConfigurationBuilder(EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
        // Initialize default serializer according to TMessage type parameter
        if (typeof(IBinaryMessage).IsAssignableFrom(typeof(TMessage)))
            ProduceBinaryMessages();
        else
            SerializeAsJson();
    }

    /// <summary>
    ///     Specifies the <see cref="IMessageSerializer" /> to be used to serialize the messages.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder SerializeUsing(IMessageSerializer serializer) => UseSerializer(serializer);

    /// <summary>
    ///     Specifies the strategy to be used to produce the messages.
    /// </summary>
    /// <param name="strategy">
    ///     The <see cref="IProduceStrategy" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder UseStrategy(IProduceStrategy strategy)
    {
        _strategy = Check.NotNull(strategy, nameof(strategy));
        return This;
    }

    /// <summary>
    ///     Specifies that the <see cref="DefaultProduceStrategy" /> has to be used, producing directly to the message broker.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ProduceDirectly()
    {
        _strategy = new DefaultProduceStrategy();
        return This;
    }

    /// <summary>
    ///     Specifies that the<see cref="OutboxProduceStrategy" /> has to be used, storing the messages into the transactional outbox table.
    ///     The operation is therefore included in the database transaction applying the message side effects to the local database.
    ///     The <see cref="IOutboxWorker" /> takes care of asynchronously sending the messages to the message broker.
    /// </summary>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ProduceToOutbox()
    {
        _strategy = new OutboxProduceStrategy();
        return This;
    }

    /// <summary>
    ///     Enables chunking, splitting the larger messages into smaller chunks.
    /// </summary>
    /// <param name="chunkSize">
    ///     The maximum chunk size in bytes.
    /// </param>
    /// <param name="alwaysAddHeaders">
    ///     A value indicating whether the <c>x-chunk-index</c> and related headers have to be added to the produced message in any case,
    ///     even if its size doesn't exceed the single chunk size. The default is <c>true</c>.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder EnableChunking(int chunkSize, bool alwaysAddHeaders = true)
    {
        if (chunkSize <= 1)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), chunkSize, "chunkSize must be greater or equal to 1.");

        _chunkSize = chunkSize;
        _alwaysAddChunkHeaders = alwaysAddHeaders;

        return This;
    }

    /// <summary>
    ///     Adds the specified message enricher.
    /// </summary>
    /// <param name="enricher">
    ///     The <see cref="IOutboundMessageEnricher" /> to be added.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddMessageEnricher(IOutboundMessageEnricher enricher)
    {
        _messageEnrichers.Add(Check.NotNull(enricher, nameof(enricher)));
        return This;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.Build" />
    public sealed override TConfiguration Build()
    {
        TConfiguration endpoint = base.Build();

        return endpoint with
        {
            Strategy = _strategy ?? endpoint.Strategy,
            Chunk = _chunkSize == null
                ? endpoint.Chunk
                : new ChunkSettings
                {
                    Size = _chunkSize.Value,
                    AlwaysAddHeaders = _alwaysAddChunkHeaders ?? true
                },
            MessageEnrichers = _messageEnrichers.Union(endpoint.MessageEnrichers).AsValueReadOnlyCollection()
        };
    }
}
