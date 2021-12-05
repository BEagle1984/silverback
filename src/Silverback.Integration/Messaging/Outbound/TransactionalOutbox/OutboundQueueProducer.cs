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
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}" />
public class OutboundQueueProducer : Producer<TransactionalOutboxBroker, ProducerConfiguration, ProducerEndpoint>
{
    private readonly IOutboxWriter _queueWriter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboundQueueProducer" /> class.
    /// </summary>
    /// <param name="queueWriter">
    ///     The <see cref="IOutboxWriter" /> to be used to write to the queue.
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
    public OutboundQueueProducer(
        IOutboxWriter queueWriter,
        TransactionalOutboxBroker broker,
        ProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IOutboundLogger<Producer<TransactionalOutboxBroker, ProducerConfiguration, ProducerEndpoint>> logger)
        : base(broker, configuration, behaviorsProvider, envelopeFactory, serviceProvider, logger)
    {
        _queueWriter = queueWriter;
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

        await _queueWriter.WriteAsync(
                message,
                messageBytes,
                headers,
                Configuration.RawName,
                Configuration.FriendlyName,
                await GetSerializedEndpointAsync(endpoint).ConfigureAwait(false))
            .ConfigureAwait(false);

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

        await _queueWriter.WriteAsync(
                message,
                messageBytes,
                headers,
                Configuration.RawName,
                Configuration.FriendlyName,
                await GetSerializedEndpointAsync(endpoint).ConfigureAwait(false))
            .ConfigureAwait(false);

        onSuccess.Invoke(null);
    }

    private async ValueTask<byte[]?> GetSerializedEndpointAsync(ProducerEndpoint endpoint) =>
        Configuration.Endpoint is IDynamicProducerEndpointResolver dynamicEndpointProvider
            ? await dynamicEndpointProvider.SerializeAsync(endpoint).ConfigureAwait(false)
            : null;
}
