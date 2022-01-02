// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories.Model;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing;

public class ProduceBehaviorTests
{
    private readonly ProduceBehavior _behavior;

    private readonly InMemoryOutbox _outbox;

    private readonly TestBroker _broker;

    public ProduceBehaviorTests()
    {
        ServiceCollection services = new();

        services.AddSilverback()
            .WithConnectionToMessageBroker(
                options => options
                    .AddBroker<TestBroker>()
                    .AddOutbox<InMemoryOutbox>());

        services
            .AddSingleton(Substitute.For<IHostApplicationLifetime>())
            .AddFakeLogger();

        ServiceProvider? serviceProvider = services.BuildServiceProvider();

        _behavior = (ProduceBehavior)serviceProvider.GetServices<IBehavior>()
            .First(behavior => behavior is ProduceBehavior);
        _broker = serviceProvider.GetRequiredService<TestBroker>();
        _outbox = (InMemoryOutbox)serviceProvider.GetRequiredService<IOutboxWriter>();
    }

    [Fact]
    public async Task HandleAsync_OutboundMessage_RelayedToEndpoint()
    {
        OutboundEnvelope<TestEventOne> outboundEnvelope = new(
            new TestEventOne(),
            Array.Empty<MessageHeader>(),
            new TestProducerConfiguration("test").GetDefaultEndpoint());

        await _behavior.HandleAsync(
            outboundEnvelope,
            message => Task.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await _behavior.HandleAsync(
            outboundEnvelope,
            message => Task.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await _behavior.HandleAsync(
            outboundEnvelope,
            message => Task.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await _outbox.CommitAsync();

        IReadOnlyCollection<OutboxStoredMessage> queued = await _outbox.ReadAsync(10);
        queued.Should().BeEmpty();
        _broker.ProducedMessages.Should().HaveCount(3);
    }

    [Fact]
    public async Task HandleAsync_OutboundMessageWithOutboxStrategy_RelayedToOutbox()
    {
        OutboundEnvelope<TestEventOne> outboundEnvelope = new(
            new TestEventOne(),
            Array.Empty<MessageHeader>(),
            new TestProducerConfiguration("test")
            {
                Strategy = new OutboxProduceStrategy()
            }.GetDefaultEndpoint());

        await _behavior.HandleAsync(
            outboundEnvelope,
            message => Task.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await _behavior.HandleAsync(
            outboundEnvelope,
            message => Task.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await _behavior.HandleAsync(
            outboundEnvelope,
            message => Task.FromResult(new[] { message }.AsReadOnlyCollection())!);
        await _outbox.CommitAsync();

        IReadOnlyCollection<OutboxStoredMessage> queued = await _outbox.ReadAsync(10);
        queued.Should().HaveCount(3);
        _broker.ProducedMessages.Should().BeEmpty();
    }
}
