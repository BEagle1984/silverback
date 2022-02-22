// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Outbound;

/// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}" />
internal class DelegatedProducer : Producer<DelegatedProducer.DelegatedBroker, ProducerConfiguration, ProducerEndpoint>
{
    private static readonly DelegatedBroker DelegatedBrokerInstance = new();

    private readonly ProduceDelegate _delegate;

    public DelegatedProducer(ProduceDelegate produceDelegate, ProducerConfiguration configuration, IServiceProvider serviceProvider)
        : base(
            DelegatedBrokerInstance,
            configuration,
            serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IOutboundLogger<Producer<DelegatedBroker, ProducerConfiguration, ProducerEndpoint>>>())
    {
        _delegate = Check.NotNull(produceDelegate, nameof(produceDelegate));
    }

    protected override Task ConnectCoreAsync() =>
        throw new NotSupportedException("Don't call this method.");

    protected override Task DisconnectCoreAsync() =>
        throw new NotSupportedException("Don't call this method.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TBroker,TConfiguration,TEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

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

        await _delegate.Invoke(message, messageBytes, headers, endpoint).ConfigureAwait(false);

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

        await _delegate.Invoke(message, messageBytes, headers, endpoint).ConfigureAwait(false);

        onSuccess.Invoke(null);
    }

    internal class DelegatedBroker : IBroker
    {
        public Type ProducerConfigurationType { get; } = typeof(ProducerConfiguration);

        public Type ConsumerConfigurationType { get; } = typeof(ConsumerConfiguration);

        public IReadOnlyList<IProducer> Producers { get; } = Array.Empty<IProducer>();

        public IReadOnlyList<IConsumer> Consumers { get; } = Array.Empty<IConsumer>();

        public bool IsConnected => false;

        public Task<IProducer> GetProducerAsync(ProducerConfiguration configuration) => throw new NotSupportedException();

        public IProducer GetProducer(ProducerConfiguration configuration) => throw new NotSupportedException();

        public IProducer GetProducer(string endpointName) => throw new NotSupportedException();

        public IConsumer AddConsumer(ConsumerConfiguration configuration) => throw new NotSupportedException();

        public Task ConnectAsync() => throw new NotSupportedException();

        public Task DisconnectAsync() => throw new NotSupportedException();
    }
}
