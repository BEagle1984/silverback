// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class StaticPartitionAssignmentTests : KafkaTestFixture
{
    public StaticPartitionAssignmentTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task StaticPartitionAssignment_PartitionsAssigned()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo("topic1", 1))
                            .AddOutbound<TestEventTwo>(
                                producer => producer
                                    .ProduceTo("topic1", 2))
                            .AddOutbound<TestEventThree>(
                                producer => producer
                                    .ProduceTo("topic2", 1))
                            .AddOutbound<TestEventFour>(
                                producer => producer
                                    .ProduceTo("topic2", 2))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(
                                        new TopicPartition("topic1", 2),
                                        new TopicPartition("topic2", 2))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());
        await publisher.PublishAsync(new TestEventFour());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
    }

    [Fact]
    public async Task StaticPartitionAssignment_UsingResolver_PartitionsAssigned()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo("topic1", 0))
                            .AddOutbound<TestEventTwo>(
                                producer => producer
                                    .ProduceTo("topic1", 1))
                            .AddOutbound<TestEventThree>(
                                producer => producer
                                    .ProduceTo("topic1", 2))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(
                                        "topic1",
                                        partitions => partitions)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
    }

    [Fact]
    public async Task StaticPartitionAssignment_UsingResolverForMultipleTopics_PartitionsAssigned()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo("topic1", 1))
                            .AddOutbound<TestEventTwo>(
                                producer => producer
                                    .ProduceTo("topic1", 2))
                            .AddOutbound<TestEventThree>(
                                producer => producer
                                    .ProduceTo("topic2", 1))
                            .AddOutbound<TestEventFour>(
                                producer => producer
                                    .ProduceTo("topic2", 2))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(
                                        new[] { "topic1", "topic2" },
                                        partitions => partitions)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());
        await publisher.PublishAsync(new TestEventFour());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
    }
}
