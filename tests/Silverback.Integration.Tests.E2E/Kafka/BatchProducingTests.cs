// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BatchProducingTests : KafkaTests
{
    public BatchProducingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task PublishAsync_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .EnableSubscribing()))) // Enable subscribing to ensure we don't fall into a mortal loop
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new TestEventOne[] { new(), new() });
        await publisher.PublishAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() }.AsEnumerable());

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventOne>>().Count().ShouldBe(2);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<IIntegrationEvent>>().Count().ShouldBe(4);
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType())
            .ShouldBe([typeof(TestEventOne), typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventOne)]);
        DefaultTopic.MessagesCount.ShouldBe(4);
    }

    [Fact]
    public async Task PublishAsync_ShouldProduceAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpy());

        static async IAsyncEnumerable<IIntegrationEvent> GetMessagesAsync()
        {
            yield return new TestEventOne();
            await Task.Delay(10);
            yield return new TestEventTwo();
            await Task.Delay(20);
            yield return new TestEventThree();
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(GetMessagesAsync());

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes.ShouldAllBe(envelope => envelope is IOutboundEnvelope<IIntegrationEvent>);
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType())
            .ShouldBe([typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventThree)]);
        DefaultTopic.MessagesCount.ShouldBe(3);
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint
                    .ProduceTo(DefaultTopicName)
                    .EnableSubscribing()))) // Enable subscribing to ensure we don't fall into a mortal loop
            .AddIntegrationSpy());

        IIntegrationPublisher publisher = Host.ServiceProvider.GetRequiredService<IIntegrationPublisher>();
        await publisher.WrapAndPublishBatchAsync([new TestEventOne(), new TestEventOne()]);
        await publisher.WrapAndPublishBatchAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() }.AsEnumerable());

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventOne>>().Count().ShouldBe(2);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<IIntegrationEvent>>().Count().ShouldBe(4);
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType())
            .ShouldBe([typeof(TestEventOne), typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventOne)]);
        DefaultTopic.MessagesCount.ShouldBe(4);
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpy());

        static async IAsyncEnumerable<IIntegrationEvent> GetMessagesAsync()
        {
            yield return new TestEventOne();
            await Task.Delay(10);
            yield return new TestEventTwo();
            await Task.Delay(20);
            yield return new TestEventThree();
        }

        IIntegrationPublisher publisher = Host.ServiceProvider.GetRequiredService<IIntegrationPublisher>();
        await publisher.WrapAndPublishBatchAsync(GetMessagesAsync());

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes.ShouldAllBe(envelope => envelope is IOutboundEnvelope<IIntegrationEvent>);
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType())
            .ShouldBe([typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventThree)]);
        DefaultTopic.MessagesCount.ShouldBe(3);
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldContinueConsumedTraces_WhenStreamingConsumedAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(5))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, IIntegrationPublisher>(HandleBatch)
            .AddIntegrationSpy());

        static Task HandleBatch(IAsyncEnumerable<TestEventOne> batch, IIntegrationPublisher publisher) =>
            publisher.WrapAndPublishBatchAsync(batch, message => new TestEventTwo { ContentEventTwo = message.ContentEventOne });

        List<string> expectedTraceIds = await ProduceTracedMessagesAsync(10);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        GetProducedTraceIds<TestEventTwo>().ShouldBe(expectedTraceIds);
    }

    [Fact]
    public async Task WrapAndPublishBatch_ShouldContinueConsumedTraces_WhenStreamingConsumedBatch()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(5))))
            .AddDelegateSubscriber<IEnumerable<TestEventOne>, IIntegrationPublisher>(HandleBatch)
            .AddIntegrationSpy());

        static void HandleBatch(IEnumerable<TestEventOne> batch, IIntegrationPublisher publisher) =>
            publisher.WrapAndPublishBatch(batch, message => new TestEventTwo { ContentEventTwo = message.ContentEventOne });

        List<string> expectedTraceIds = await ProduceTracedMessagesAsync(10);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        GetProducedTraceIds<TestEventTwo>().ShouldBe(expectedTraceIds);
    }

    private async Task<List<string>> ProduceTracedMessagesAsync(int count)
    {
        List<string> traceIds = [];
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= count; i++)
        {
            using Activity activity = new("TestActivity");
            activity.Start();
            traceIds.Add(activity.TraceId.ToString());

            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        return traceIds;
    }

    private List<string> GetProducedTraceIds<TMessage>() =>
    [
        .. Helper.Spy.OutboundEnvelopes
            .OfType<IOutboundEnvelope<TMessage>>()
            .Select(envelope => ActivityContext.Parse(envelope.Headers[DefaultMessageHeaders.TraceId]!, null).TraceId.ToString())
    ];

    // Note: Tests for batch producing via outbox are implemented in the Outbox*Tests
}
