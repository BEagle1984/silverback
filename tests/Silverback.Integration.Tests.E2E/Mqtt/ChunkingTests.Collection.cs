// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ChunkingTests
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Chunking_ShouldProduceAllChunksBeforeCompleting_WhenPublishingEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(CreateLongMessages(10).AsEnumerable()).WaitAsync(PublishTimeout);

        // All chunks must be published by the time the publishing completes
        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(10);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(20);
        GetDefaultTopicMessages().Count.ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAllChunksBeforeCompleting_WhenPublishingAsyncEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(GetLongMessagesSlowlyAsync(10)).WaitAsync(PublishTimeout);

        // All chunks must be published by the time the publishing completes
        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(10);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(20);
        GetDefaultTopicMessages().Count.ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count);
    }

    [Fact]
    public async Task Chunking_ShouldThrowAfterAllChunksAreProduced_WhenPublishingEnumerableAndSingleChunkFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))))
            .AddSingletonBrokerBehavior(new FailingChunkProducerBehavior("Long message 2", 1))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishAsync(CreateLongMessages(10).AsEnumerable()).WaitAsync(PublishTimeout);

        // All other chunks must be published by the time the error is reported
        await act.ShouldThrowAsync<ProduceException>();
        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(10);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(20);
        GetDefaultTopicMessages().Count.ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count - 1);
    }

    [Fact]
    public async Task Chunking_ShouldThrowAfterAllChunksAreProduced_WhenPublishingAsyncEnumerableAndSingleChunkFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(ChunkSize))))
            .AddSingletonBrokerBehavior(new FailingChunkProducerBehavior("Long message 2", 1))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishAsync(GetLongMessagesSlowlyAsync(5)).WaitAsync(PublishTimeout);

        // The other messages must still be published by the time the error is reported
        await act.ShouldThrowAsync<ProduceException>();
        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(10);
        GetDefaultTopicMessages().Count.ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count - 1);
    }

    private static TestEventOne[] CreateLongMessages(int count) =>
    [
        .. Enumerable.Range(1, count).Select(i => new TestEventOne { ContentEventOne = $"Long message {i}" })
    ];

    // Waits for the chunks of each message to be published before the next message is enumerated
    private static async IAsyncEnumerable<TestEventOne> GetLongMessagesSlowlyAsync(int count)
    {
        foreach (TestEventOne message in CreateLongMessages(count))
        {
            await Task.Delay(50);
            yield return message;
        }
    }

    // Sets an invalid correlation data header on the matching chunk, to make the publishing of that chunk fail
    private sealed class FailingChunkProducerBehavior : IProducerBehavior
    {
        private readonly string _content;

        private readonly int _chunkIndex;

        public FailingChunkProducerBehavior(string content, int chunkIndex)
        {
            _content = content;
            _chunkIndex = chunkIndex;
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Sequencer + 1;

        public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            if (context.Envelope.Message is TestEventOne message && message.ContentEventOne == _content &&
                context.Envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex) == _chunkIndex.ToString(CultureInfo.InvariantCulture))
            {
                context.Envelope.Headers.AddOrReplace(MqttMessageHeaders.CorrelationData, "not base64");
            }

            return next(context, cancellationToken);
        }
    }
}
