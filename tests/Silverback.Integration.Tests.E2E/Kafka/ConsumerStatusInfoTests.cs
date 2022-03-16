// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ConsumerStatusInfoTests : KafkaTestFixture
{
    public ConsumerStatusInfoTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task StatusInfo_ConsumingAndRebalanceAndDisconnecting_StatusIsCorrectlySet()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromMilliseconds(100))))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run(waitUntilBrokerConnected: false);

        IConsumer consumer = Helper.Broker.Consumers[0];

        consumer.IsConnected.Should().BeTrue();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Ready);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);

        DefaultConsumerGroup.Rebalance();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Ready);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.IsConnected.Should().BeTrue();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

        await Helper.Broker.DisconnectAsync();

        consumer.IsConnected.Should().BeFalse();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Disconnected);
    }

    [Fact]
    public async Task StatusInfo_StaticPartitionsAssignment_StatusIsCorrectlySet()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(
                                        new TopicPartition(DefaultTopicName, 0),
                                        new TopicPartition(DefaultTopicName, 1),
                                        new TopicPartition(DefaultTopicName, 2),
                                        new TopicPartition(DefaultTopicName, 3),
                                        new TopicPartition(DefaultTopicName, 4))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run(waitUntilBrokerConnected: false);

        IConsumer consumer = Helper.Broker.Consumers[0];

        consumer.IsConnected.Should().BeTrue();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.IsConnected.Should().BeTrue();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

        await Helper.Broker.DisconnectAsync();

        consumer.IsConnected.Should().BeFalse();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Disconnected);
    }

    [Fact]
    public async Task StatusInfo_PollTimeoutWithoutAutoRecovery_StatusReverted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultGroupId)
                                            .DisableAutoRecovery())))
                    .AddIntegrationSpyAndSubscriber())
            .Run(waitUntilBrokerConnected: false);

        KafkaConsumer consumer = (KafkaConsumer)Helper.Broker.Consumers[0];

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Ready);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);

        // Simulate a local poll timeout
        KafkaConsumerLocalTimeoutMonitor timeoutMonitor = (KafkaConsumerLocalTimeoutMonitor)Host.ServiceProvider
            .GetServices<IBrokerCallback>()
            .Single(service => service is KafkaConsumerLocalTimeoutMonitor);
        timeoutMonitor.OnConsumerLog(
            new LogMessage(
                "rdkafka#consumer-1",
                SyslogLevel.Warning,
                "MAXPOLL",
                "[thrd:main]: Application maximum poll interval (10000ms) exceeded by 89ms (adjust max.poll.interval.ms for long-running message processing): leaving group"),
            consumer);

        consumer.IsConnected.Should().BeTrue();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);
    }

    [Fact]
    public async Task StatusInfo_PollTimeoutWithAutoRecovery_Reconnected()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultGroupId)
                                            .EnableAutoRecovery())))
                    .AddIntegrationSpyAndSubscriber())
            .Run(waitUntilBrokerConnected: false);

        KafkaConsumer consumer = (KafkaConsumer)Helper.Broker.Consumers[0];

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Ready);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);

        // Simulate a local poll timeout
        KafkaConsumerLocalTimeoutMonitor timeoutMonitor = (KafkaConsumerLocalTimeoutMonitor)Host.ServiceProvider
            .GetServices<IBrokerCallback>()
            .Single(service => service is KafkaConsumerLocalTimeoutMonitor);
        timeoutMonitor.OnConsumerLog(
            new LogMessage(
                "rdkafka#consumer-1",
                SyslogLevel.Warning,
                "MAXPOLL",
                "[thrd:main]: Application maximum poll interval (10000ms) exceeded by 89ms (adjust max.poll.interval.ms for long-running message processing): leaving group"),
            consumer);

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.History.Count >= 5);

        consumer.IsConnected.Should().BeTrue();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);
        string.Join("->", consumer.StatusInfo.History.TakeLast(3).Select(change => change.Status))
            .Should().BeEquivalentTo("Disconnected->Connected->Ready");
    }

    [Fact]
    public async Task StatusInfo_ConsumingAndDisconnecting_StatusHistoryRecorded()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromMilliseconds(100))))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run(waitUntilBrokerConnected: false);

        IConsumer consumer = Helper.Broker.Consumers[0];

        consumer.StatusInfo.History.Should().HaveCount(1);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Connected);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.History.Count >= 2);

        consumer.StatusInfo.History.Should().HaveCount(2);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Ready);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.History.Should().HaveCount(3);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Consuming);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        await Helper.Broker.DisconnectAsync();

        consumer.StatusInfo.History.Should().HaveCount(4);
        consumer.StatusInfo.History.Last().Status.Should()
            .Be(ConsumerStatus.Disconnected);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task StatusInfo_Consuming_LatestConsumedMessageTracked()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Broker.Consumers[0].StatusInfo.LatestConsumedMessageIdentifier.Should().BeOfType<KafkaOffset>();
        Helper.Broker.Consumers[0].StatusInfo.LatestConsumedMessageIdentifier.As<KafkaOffset>().Offset.Value.Should().Be(1);
        Helper.Broker.Consumers[0].StatusInfo.LatestConsumedMessageTimestamp.Should().NotBeNull();
    }
}
