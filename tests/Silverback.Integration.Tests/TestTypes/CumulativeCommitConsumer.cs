// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes;

// A consumer whose commit implicitly commits the previous messages, like Kafka (public, to be substituted)
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Substituted")]
public abstract class CumulativeCommitConsumer : IConsumer, ICumulativeCommitConsumer
{
    public abstract string Name { get; }

    public abstract string DisplayName { get; }

    public abstract IBrokerClient Client { get; }

    public abstract IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; }

    public abstract IConsumerStatusInfo StatusInfo { get; }

    public abstract ValueTask TriggerReconnectAsync();

    public abstract ValueTask StartAsync();

    public abstract ValueTask StopAsync(bool waitUntilStopped = true);

    public abstract ValueTask StopAsync(IBrokerMessageIdentifier brokerMessageIdentifier, bool waitUntilStopped = true);

    public abstract ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier);

    public abstract ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

    public abstract ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier);

    public abstract ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

    public abstract int IncrementFailedAttempts(IRawInboundEnvelope envelope);
}
