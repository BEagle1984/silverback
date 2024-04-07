// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BatchProducingFixture : KafkaFixture
{
    public BatchProducingFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task BatchProducing_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventsAsync(new[] { new TestEventOne(), new TestEventOne() });
        await publisher.PublishEventsAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventOne>>().Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventTwo>>().Should().HaveCount(1);
        DefaultTopic.MessagesCount.Should().Be(4);
    }

    [Fact]
    public async Task BatchProducing_ShouldProduceAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
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

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventsAsync(GetMessagesAsync());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventOne>>().Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventTwo>>().Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventThree>>().Should().HaveCount(1);
        DefaultTopic.MessagesCount.Should().Be(3);
    }

    [Fact]
    public async Task BatchProducing_ShouldProduceBatchWithHeaders()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventsAsync(
            new[]
            {
                new TestEventOne().AddHeader("header1", "value1").AddHeader("header2", "value2"),
                new TestEventOne().AddHeader("header3", "value3")
            });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "header1" && header.Value == "value1");
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "header2" && header.Value == "value2");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should()
            .ContainSingle(header => header.Name == "header3" && header.Value == "value3");
        DefaultTopic.MessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task BatchProducing_ShouldProduceBatchViaOutbox()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddInMemoryOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseMemory())
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseMemory())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new[] { new TestEventOne(), new TestEventOne() });
        await publisher.PublishAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().Should().HaveCount(1);
    }

    [Fact]
    public async Task BatchProducing_ShouldProduceAsyncBatchViaOutbox()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddInMemoryOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseMemory())
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseMemory())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static async IAsyncEnumerable<IIntegrationEvent> GetMessagesAsync()
        {
            yield return new TestEventOne();
            await Task.Delay(10);
            yield return new TestEventTwo();
            await Task.Delay(20);
            yield return new TestEventThree();
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(GetMessagesAsync());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventThree>>().Should().HaveCount(1);
    }
}
