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
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingTests
{
    private const int CollectionChunkSize = 10;

    private const int CollectionChunksPerMessage = 4;

    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Chunking_ShouldProduceAllChunksBeforeCompleting_WhenPublishingEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(CollectionChunkSize))))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(CreateLongMessages(10).AsEnumerable()).WaitAsync(PublishTimeout);

        // All chunks must be acknowledged by the time the publishing completes
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(10 * CollectionChunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ShouldAllBe(envelope => envelope.BrokerMessageIdentifier != null);
        DefaultTopic.MessagesCount.ShouldBe(10 * CollectionChunksPerMessage);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAllChunksBeforeCompleting_WhenPublishingAsyncEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(CollectionChunkSize))))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(GetLongMessagesSlowlyAsync(10)).WaitAsync(PublishTimeout);

        // All chunks must be acknowledged by the time the publishing completes
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(10 * CollectionChunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ShouldAllBe(envelope => envelope.BrokerMessageIdentifier != null);
        DefaultTopic.MessagesCount.ShouldBe(10 * CollectionChunksPerMessage);
    }

    [Fact]
    public async Task Chunking_ShouldProduceRemainingMessagesAndThrow_WhenPublishingEnumerableAndAllChunksOfMessageFail()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(CollectionChunkSize))))
            .AddSingletonBrokerBehavior(new FailingChunkProducerBehavior("Long message 1", null))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishAsync(CreateLongMessages(3).AsEnumerable()).WaitAsync(PublishTimeout);

        await act.ShouldThrowAsync<ProduceException>();
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(3 * CollectionChunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.Count(envelope => envelope.BrokerMessageIdentifier != null).ShouldBe(2 * CollectionChunksPerMessage);
        DefaultTopic.MessagesCount.ShouldBe(2 * CollectionChunksPerMessage);
    }

    [Fact]
    public async Task Chunking_ShouldThrowAfterAllChunksAreProduced_WhenPublishingEnumerableAndSingleChunkFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(CollectionChunkSize))))
            .AddSingletonBrokerBehavior(new FailingChunkProducerBehavior("Long message 2", 1))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishAsync(CreateLongMessages(10).AsEnumerable()).WaitAsync(PublishTimeout);

        // All other chunks must be acknowledged by the time the error is reported
        await act.ShouldThrowAsync<ProduceException>();
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(10 * CollectionChunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.Count(envelope => envelope.BrokerMessageIdentifier != null).ShouldBe((10 * CollectionChunksPerMessage) - 1);
        DefaultTopic.MessagesCount.ShouldBe((10 * CollectionChunksPerMessage) - 1);
    }

    [Fact]
    public async Task Chunking_ShouldThrowAfterAllChunksAreProduced_WhenPublishingAsyncEnumerableAndSingleChunkFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(CollectionChunkSize))))
            .AddSingletonBrokerBehavior(new FailingChunkProducerBehavior("Long message 2", 1))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        Func<Task> act = () => publisher.PublishAsync(GetLongMessagesSlowlyAsync(5)).WaitAsync(PublishTimeout);

        // The other messages must still be produced and acknowledged by the time the error is reported
        await act.ShouldThrowAsync<ProduceException>();
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(5 * CollectionChunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.Count(envelope => envelope.BrokerMessageIdentifier != null).ShouldBe((5 * CollectionChunksPerMessage) - 1);
        DefaultTopic.MessagesCount.ShouldBe((5 * CollectionChunksPerMessage) - 1);
    }

    private static TestEventOne[] CreateLongMessages(int count) =>
    [
        .. Enumerable.Range(1, count).Select(i => new TestEventOne { ContentEventOne = $"Long message {i}" })
    ];

    // Waits longer than the mocked producer takes to acknowledge a message, so that the chunks of each message are acknowledged before
    // the next message is enumerated
    private static async IAsyncEnumerable<TestEventOne> GetLongMessagesSlowlyAsync(int count)
    {
        foreach (TestEventOne message in CreateLongMessages(count))
        {
            await Task.Delay(50);
            yield return message;
        }
    }

    // Routes the matching chunks to a partition that doesn't exist, to make the mocked producer fail them
    private sealed class FailingChunkProducerBehavior : IProducerBehavior
    {
        private readonly string _content;

        private readonly int? _chunkIndex;

        public FailingChunkProducerBehavior(string content, int? chunkIndex)
        {
            _content = content;
            _chunkIndex = chunkIndex;
        }

        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Sequencer + 1;

        public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
        {
            if (context.Envelope.Message is TestEventOne message && message.ContentEventOne == _content &&
                (_chunkIndex == null ||
                 context.Envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex) == _chunkIndex.Value.ToString(CultureInfo.InvariantCulture)))
            {
                context.Envelope = OutboundEnvelopeFactory.CreateEnvelope(
                    context.Envelope.RawMessage.ReReadAll(),
                    context.Envelope.Headers,
                    (KafkaProducerEndpointConfiguration)context.Envelope.EndpointConfiguration with
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver(DefaultTopicName, 99)
                    },
                    context.Envelope.Producer);
            }

            return next(context, cancellationToken);
        }
    }
}
