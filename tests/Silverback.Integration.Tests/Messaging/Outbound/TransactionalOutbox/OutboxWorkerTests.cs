// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxWorkerTests
{
    private readonly IOutboxWriter _outboxWriter;

    private readonly TestBroker _broker;

    private readonly IOutboxWorker _worker;

    private readonly OutboundEnvelope _sampleOutboundEnvelope;

    public OutboxWorkerTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>()
                        .AddOutbox<InMemoryOutbox>()
                        .AddOutboxWorker())
                .AddEndpoints(
                    endpoints => endpoints
                        .AddOutbound<TestEventOne>(new TestProducerConfiguration("topic1"))
                        .AddOutbound<TestEventTwo>(new TestProducerConfiguration("topic2"))
                        .AddOutbound<TestEventThree>(new TestProducerConfiguration("topic3a"))
                        .AddOutbound<TestEventThree>(new TestProducerConfiguration("topic3b"))));

        _broker = serviceProvider.GetRequiredService<TestBroker>();
        _broker.ConnectAsync().Wait();

        _worker = serviceProvider.GetRequiredService<IOutboxWorker>();
        _outboxWriter = serviceProvider.GetRequiredService<IOutboxWriter>();

        _sampleOutboundEnvelope = new OutboundEnvelope<TestEventOne>(
            new TestEventOne { Content = "Test" },
            null,
            new TestProducerConfiguration("topic1").GetDefaultEndpoint());
        _sampleOutboundEnvelope.RawMessage =
            AsyncHelper.RunSynchronously(
                () => new JsonMessageSerializer<object>().SerializeAsync(
                        _sampleOutboundEnvelope.Message,
                        _sampleOutboundEnvelope.Headers,
                        TestProducerEndpoint.GetDefault())
                    .AsTask());
    }

    [Fact]
    public async Task ProcessQueue_SomeMessages_Produced()
    {
        await _outboxWriter.WriteAsync(
            new TestEventOne { Content = "Test" },
            null,
            null,
            "topic1",
            "topic1",
            new byte[10]);
        await _outboxWriter.WriteAsync(
            new TestEventTwo { Content = "Test" },
            null,
            null,
            "topic2",
            "topic2",
            new byte[10]);
        await _outboxWriter.CommitAsync();

        await _worker.ProcessQueueAsync(CancellationToken.None);

        _broker.ProducedMessages.Should().HaveCount(2);
        _broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic1");
        _broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic2");
    }

    [Fact]
    public async Task ProcessQueue_SomeMessagesWithMultipleEndpoints_CorrectlyProduced()
    {
        await _outboxWriter.WriteAsync(
            new TestEventThree { Content = "Test" },
            null,
            null,
            "topic3a",
            "topic3a",
            new byte[10]);
        await _outboxWriter.WriteAsync(
            new TestEventThree { Content = "Test" },
            null,
            null,
            "topic3b",
            "topic3b",
            new byte[10]);
        await _outboxWriter.CommitAsync();

        await _worker.ProcessQueueAsync(CancellationToken.None);

        _broker.ProducedMessages.Should().HaveCount(2);
        _broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic3a");
        _broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic3b");
    }

    [Fact]
    public async Task ProcessQueue_RunTwice_ProducedOnce()
    {
        await _outboxWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _outboxWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _outboxWriter.CommitAsync();

        await _worker.ProcessQueueAsync(CancellationToken.None);
        await _worker.ProcessQueueAsync(CancellationToken.None);

        _broker.ProducedMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProcessQueue_RunTwice_ProducedNewMessages()
    {
        await _outboxWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _outboxWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _outboxWriter.CommitAsync();

        await _worker.ProcessQueueAsync(CancellationToken.None);

        await _outboxWriter.WriteAsync(
            _sampleOutboundEnvelope.Message,
            _sampleOutboundEnvelope.RawMessage.ReadAll(),
            _sampleOutboundEnvelope.Headers,
            _sampleOutboundEnvelope.Endpoint.Configuration.RawName,
            _sampleOutboundEnvelope.Endpoint.Configuration.FriendlyName,
            new byte[10]);
        await _outboxWriter.CommitAsync();

        await _worker.ProcessQueueAsync(CancellationToken.None);

        _broker.ProducedMessages.Should().HaveCount(3);
    }
}
