// Copyright (c) 2023 Sergio Aquilini
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
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ConsumerStatusInfoFixture : KafkaFixture
{
    public ConsumerStatusInfoFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task StatusInfo_ShouldReportCorrectStatus_WhenConsumingAndRebalanceAndDisconnecting()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromMilliseconds(100))))
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                    .AddIntegrationSpyAndSubscriber())
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Started);

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Connected);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        DefaultConsumerGroup.ScheduleRebalance();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Started);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Started);

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Connected);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StatusInfo_ShouldReportCorrectStatus_WhenStaticallyAssigningPartitions()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(
                                            new TopicPartition(DefaultTopicName, 0),
                                            new TopicPartition(DefaultTopicName, 1),
                                            new TopicPartition(DefaultTopicName, 2),
                                            new TopicPartition(DefaultTopicName, 3),
                                            new TopicPartition(DefaultTopicName, 4)))))
                .AddIntegrationSpyAndSubscriber());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        IProducer producer = Helper.GetProducer(
            producer => producer.WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)));
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StatusInfo_ShouldRevertStatus_WhenPollTimeoutWithoutAutoRecovery()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .DisableAutoRecovery()
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        KafkaConsumer consumer = (KafkaConsumer)Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Connected);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        // Simulate a local poll timeout
        KafkaConsumerLocalTimeoutMonitor timeoutMonitor = (KafkaConsumerLocalTimeoutMonitor)Host.ServiceProvider
            .GetServices<IBrokerClientCallback>()
            .Single(service => service is KafkaConsumerLocalTimeoutMonitor);
        timeoutMonitor.OnConsumerLog(
            new LogMessage(
                "rdkafka#consumer-1",
                SyslogLevel.Warning,
                "MAXPOLL",
                "[thrd:main]: Application maximum poll interval (10000ms) exceeded by 89ms (adjust max.poll.interval.ms for long-running message processing): leaving group"),
            consumer);

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Started);
    }

    [Fact]
    public async Task StatusInfo_ShouldTrackReconnect_WhenPollTimeoutWithAutoRecovery()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .EnableAutoRecovery()
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        KafkaConsumer consumer = (KafkaConsumer)Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Connected);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        // Simulate a local poll timeout
        KafkaConsumerLocalTimeoutMonitor timeoutMonitor = (KafkaConsumerLocalTimeoutMonitor)Host.ServiceProvider
            .GetServices<IBrokerClientCallback>()
            .Single(service => service is KafkaConsumerLocalTimeoutMonitor);
        timeoutMonitor.OnConsumerLog(
            new LogMessage(
                "rdkafka#consumer-1",
                SyslogLevel.Warning,
                "MAXPOLL",
                "[thrd:main]: Application maximum poll interval (10000ms) exceeded by 89ms (adjust max.poll.interval.ms for long-running message processing): leaving group"),
            consumer);

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.History.Count >= 5);

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);
        string.Join("->", consumer.StatusInfo.History.TakeLast(3).Select(change => change.Status))
            .Should().BeEquivalentTo("Stopped->Started->Connected");
    }

    [Fact]
    public async Task StatusInfo_ShouldRecordHistory_WhenConsumingAndDisconnecting()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options.AddMockedKafka(
                            mockedKafkaOptions =>
                                mockedKafkaOptions.DelayPartitionsAssignment(TimeSpan.FromMilliseconds(100))))
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                    .AddIntegrationSpyAndSubscriber())
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        consumer.StatusInfo.History.Should().HaveCount(1);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Started);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.History.Count >= 2);

        consumer.StatusInfo.History.Should().HaveCount(2);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Connected);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.History.Should().HaveCount(3);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Consuming);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.History.Should().HaveCount(4);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Stopped);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task StatusInfo_ShouldTrackLatestConsumedMessage()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options.AddMockedKafka(
                        mockedKafkaOptions =>
                            mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.LatestConsumedMessageIdentifier.Should().BeOfType<KafkaOffset>();
        consumer.StatusInfo.LatestConsumedMessageIdentifier.As<KafkaOffset>().Offset.Value.Should().Be(1);
        consumer.StatusInfo.LatestConsumedMessageTimestamp.Should().NotBeNull();
    }
}
