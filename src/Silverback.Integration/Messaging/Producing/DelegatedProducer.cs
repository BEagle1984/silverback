// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <inheritdoc cref="Producer" />
internal class DelegatedProducer : Producer
{
    private static readonly DelegatedClient DelegatedClientInstance = new();

    private readonly ProduceDelegate _delegate;

    public DelegatedProducer(ProduceDelegate produceDelegate, ProducerEndpointConfiguration endpointConfiguration, IServiceProvider serviceProvider)
        : base(
            Guid.NewGuid().ToString("D"),
            DelegatedClientInstance,
            endpointConfiguration,
            serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IProducerLogger<Producer>>())
    {
        _delegate = Check.NotNull(produceDelegate, nameof(produceDelegate));
    }

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
    protected override IBrokerMessageIdentifier ProduceCore(IOutboundEnvelope envelope) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer.ProduceCore{TState}(IOutboundEnvelope,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    protected override void ProduceCore<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) => throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
    {
        await _delegate.Invoke(envelope).ConfigureAwait(false);

        return null;
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
