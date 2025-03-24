// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Producing;

internal abstract class DelegatedProducer : Producer
{
    private static readonly DelegatedClient DelegatedClientInstance = new();

    protected DelegatedProducer(
        ProducerEndpointConfiguration endpointConfiguration,
        IServiceProvider serviceProvider)
        : base(
            Guid.NewGuid().ToString("D"),
            DelegatedClientInstance,
            endpointConfiguration,
            serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            serviceProvider,
            serviceProvider.GetRequiredService<IProducerLogger<Producer>>())
    {
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
