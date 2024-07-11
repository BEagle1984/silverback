// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
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
    public async Task PublishAsync_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .EnableSubscribing()))) // Enable subscribing to ensure we don't fall into a mortal loop
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new TestEventOne[] { new(), new() });
        await publisher.PublishAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() }.AsEnumerable());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventOne>>().Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<IIntegrationEvent>>().Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType()).Should()
            .BeEquivalentTo([typeof(TestEventOne), typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventOne)]);
        DefaultTopic.MessagesCount.Should().Be(4);
    }

    [Fact]
    public async Task PublishAsync_ShouldProduceAsyncBatch()
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
        await publisher.PublishAsync(GetMessagesAsync());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes.Should().AllBeAssignableTo<IOutboundEnvelope<IIntegrationEvent>>();
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType()).Should()
            .BeEquivalentTo([typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventThree)]);
        DefaultTopic.MessagesCount.Should().Be(3);
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .EnableSubscribing()))) // Enable subscribing to ensure we don't fall into a mortal loop
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync([new TestEventOne(), new TestEventOne()]);
        await publisher.WrapAndPublishBatchAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() }.AsEnumerable());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<TestEventOne>>().Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes.OfType<IOutboundEnvelope<IIntegrationEvent>>().Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType()).Should()
            .BeEquivalentTo([typeof(TestEventOne), typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventOne)]);
        DefaultTopic.MessagesCount.Should().Be(4);
    }

    [Fact]
    public async Task WrapAndPublishAsync_ShouldProduceAsyncBatch()
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
        await publisher.WrapAndPublishBatchAsync(GetMessagesAsync());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes.Should().AllBeAssignableTo<IOutboundEnvelope<IIntegrationEvent>>();
        Helper.Spy.OutboundEnvelopes.Select(envelope => envelope.Message?.GetType()).Should()
            .BeEquivalentTo([typeof(TestEventOne), typeof(TestEventTwo), typeof(TestEventThree)]);
        DefaultTopic.MessagesCount.Should().Be(3);
    }

    // Note: Tests for batch producing via outbox are implemented in the Outbox*Fixture
}
