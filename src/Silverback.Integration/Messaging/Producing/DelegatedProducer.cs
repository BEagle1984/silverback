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
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <inheritdoc cref="Producer{TEndpoint}" />
internal class DelegatedProducer : Producer<ProducerEndpoint>
{
    private static readonly DelegatedClient DelegatedClientInstance = new();

    private readonly ProduceDelegate _delegate;

    public DelegatedProducer(ProduceDelegate produceDelegate, ProducerEndpointConfiguration endpointConfiguration, IServiceProvider serviceProvider)
        : base(
            Guid.NewGuid().ToString("D"),
            DelegatedClientInstance,
            endpointConfiguration,
            serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            serviceProvider.GetRequiredService<IOutboundEnvelopeFactory>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IProducerLogger<Producer<ProducerEndpoint>>>())
    {
        _delegate = Check.NotNull(produceDelegate, nameof(produceDelegate));
    }

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier ProduceCore(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override IBrokerMessageIdentifier ProduceCore(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCore(byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(Stream,IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint) =>
        await ProduceCoreAsync(
                await message.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint)
            .ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(byte[],IReadOnlyCollection{MessageHeader},TEndpoint)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        await _delegate.Invoke(message, headers, endpoint).ConfigureAwait(false);

        return null;
    }

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(Stream,IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async ValueTask ProduceCoreAsync(
        Stream? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        await ProduceCoreAsync(
                await message.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint,
                onSuccess,
                onError)
            .ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TEndpoint}.ProduceCoreAsync(byte[],IReadOnlyCollection{MessageHeader},TEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async ValueTask ProduceCoreAsync(
        byte[]? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        Check.NotNull(onSuccess, nameof(onSuccess));
        Check.NotNull(onError, nameof(onError));

        await _delegate.Invoke(message, headers, endpoint).ConfigureAwait(false);

        onSuccess.Invoke(null);
    }

    private sealed class DelegatedClient : IBrokerClient
    {
        public AsyncEvent<BrokerClient> Initialized { get; } = new();

        public string Name => string.Empty;

        public string DisplayName => string.Empty;

        public AsyncEvent<BrokerClient> Initializing { get; } = new();

        public AsyncEvent<BrokerClient> Disconnecting { get; } = new();

        public AsyncEvent<BrokerClient> Disconnected { get; } = new();

        public ClientStatus Status => ClientStatus.Initialized;

        public void Dispose()
        {
            // Nothing to dispose
        }

        public ValueTask ConnectAsync() => default;

        public ValueTask DisconnectAsync() => default;

        public ValueTask ReconnectAsync() => default;

        public ValueTask DisposeAsync() => default;
    }
}
