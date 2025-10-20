// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types;

public class TestConsumer : IConsumer
{
    public TestConsumer()
    {
        EnvelopeFactory = new TestInboundEnvelopeFactory(this);
    }

    public string Name => "consumer1-name";

    public string DisplayName => "consumer1";

    public IBrokerClient Client { get; } = new TestClient();

    public IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; } = [];

    public IInboundEnvelopeFactory EnvelopeFactory { get; }

    public IConsumerStatusInfo StatusInfo { get; } = new ConsumerStatusInfo();

    public ValueTask TriggerReconnectAsync() => throw new NotSupportedException();

    public ValueTask StartAsync() => throw new NotSupportedException();

    public ValueTask StopAsync(bool waitUntilStopped = true) => throw new NotSupportedException();

    public ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

    public ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

    public ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

    public ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

    public int IncrementFailedAttempts(IInboundEnvelope envelope) => throw new NotSupportedException();
}
