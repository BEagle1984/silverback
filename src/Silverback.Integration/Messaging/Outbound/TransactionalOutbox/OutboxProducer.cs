// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}" />
public class OutboxProducer : Producer<OutboxBroker, ProducerConfiguration, ProducerEndpoint>
{
    private readonly IOutboxWriter _outboxWriter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProducer" /> class.
    /// </summary>
    /// <param name="outboxWriter">
    ///     The <see cref="IOutboxWriter" />.
    /// </param>
    /// <param name="broker">
    ///     The <see cref="IBroker" /> that instantiated this producer.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="ProducerConfiguration" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="envelopeFactory">
    ///     The <see cref="IOutboundEnvelopeFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IOutboundLogger{TCategoryName}" />.
    /// </param>
    public OutboxProducer(
        IOutboxWriter outboxWriter,
        OutboxBroker broker,
        ProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IOutboundLogger<Producer<OutboxBroker, ProducerConfiguration, ProducerEndpoint>> logger)
        : base(broker, configuration, behaviorsProvider, envelopeFactory, serviceProvider, logger)
    {
        _outboxWriter = outboxWriter;
    }

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        throw new InvalidOperationException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        throw new InvalidOperationException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        throw new InvalidOperationException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        throw new InvalidOperationException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint)
            .ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        await AddToOutboxAsync(message, messageBytes, headers, endpoint).ConfigureAwait(false);

        return null;
    }

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async Task ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint,
                onSuccess,
                onError)
            .ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async Task ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        Check.NotNull(onSuccess, nameof(onSuccess));
        Check.NotNull(onError, nameof(onError));

        await AddToOutboxAsync(message, messageBytes, headers, endpoint).ConfigureAwait(false);

        onSuccess.Invoke(null);
    }

    private async Task AddToOutboxAsync(object? message, byte[]? messageBytes, IReadOnlyCollection<MessageHeader>? headers, ProducerEndpoint endpoint) =>
        await _outboxWriter.AddAsync(
                new OutboxMessage(
                    message?.GetType(),
                    messageBytes,
                    headers,
                    new OutboxMessageEndpoint(
                        Configuration.RawName,
                        Configuration.FriendlyName,
                        await GetSerializedEndpointAsync(endpoint).ConfigureAwait(false))))
            .ConfigureAwait(false);

    private async ValueTask<byte[]?> GetSerializedEndpointAsync(ProducerEndpoint endpoint) =>
        Configuration.Endpoint is IDynamicProducerEndpointResolver dynamicEndpointProvider
            ? await dynamicEndpointProvider.SerializeAsync(endpoint).ConfigureAwait(false)
            : null;
}
