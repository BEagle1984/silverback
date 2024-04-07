// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class MultipleBrokersFixture : KafkaFixture
{
    public MultipleBrokersFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task MultipleBrokers_ShouldCorrectlyProduceAndConsume_WhenTopicNamesAreOverlapping()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e-1")
                        .AddProducer(
                            producer => producer
                                .Produce<Broker1Message>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group1")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e-2")
                        .AddProducer(
                            producer => producer
                                .Produce<Broker2Message>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group2")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync(); // Wait twice to ensure ordering in asserts

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0].Consumer.As<KafkaConsumer>().Configuration.BootstrapServers.Should().Be("PLAINTEXT://e2e-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1].Consumer.As<KafkaConsumer>().Configuration.BootstrapServers.Should().Be("PLAINTEXT://e2e-2");
    }

    [Fact]
    public async Task MultipleBrokers_ShouldCorrectlyProduceAndConsume_WhenTopicNamesAndGroupIdsAreOverlapping()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e-1")
                        .AddProducer(
                            producer => producer
                                .Produce<Broker1Message>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e-2")
                        .AddProducer(
                            producer => producer
                                .Produce<Broker2Message>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new Broker1Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync(); // Wait twice to ensure ordering in asserts

        await publisher.PublishAsync(new Broker2Message());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Broker1Message>();
        Helper.Spy.InboundEnvelopes[0].Consumer.As<KafkaConsumer>().Configuration.BootstrapServers.Should().Be("PLAINTEXT://e2e-1");
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Broker2Message>();
        Helper.Spy.InboundEnvelopes[1].Consumer.As<KafkaConsumer>().Configuration.BootstrapServers.Should().Be("PLAINTEXT://e2e-2");
    }

    private sealed class Broker1Message : IIntegrationMessage;

    private sealed class Broker2Message : IIntegrationMessage;
}
