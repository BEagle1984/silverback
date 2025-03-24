// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointFixture : KafkaFixture
{
    public ConsumerEndpointFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeSequentially()
    {
        int[] receivedMessages = [0, 0, 0];
        int[] exitedSubscribers = [0, 0, 0];
        bool areOverlapping = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(HandleEnvelope));

        async ValueTask HandleEnvelope(IInboundEnvelope<TestEventOne> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            int partitionIndex = offset.TopicPartition.Partition;

            if (receivedMessages[partitionIndex] != exitedSubscribers[partitionIndex])
                areOverlapping = true;

            receivedMessages[partitionIndex]++;

            await Task.Delay(100);

            exitedSubscribers[partitionIndex]++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        areOverlapping.ShouldBeFalse();
        receivedMessages.Sum().ShouldBe(10);
        exitedSubscribers.Sum().ShouldBe(10);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenMultipleConsumersForDifferentTopicsAreConfigured()
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer1 = Helper.GetProducerForEndpoint("topic1");
        IProducer producer2 = Helper.GetProducerForEndpoint("topic2");

        for (int i = 1; i <= 5; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);

        IInboundEnvelope<TestEventOne>[] eventOneEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        IInboundEnvelope<TestEventTwo>[] eventTwoEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().ToArray();

        eventOneEnvelopes.Length.ShouldBe(5);
        eventOneEnvelopes.Select(envelope => envelope.Endpoint.RawName).ShouldAllBe(rawName => rawName == "topic1");
        eventOneEnvelopes.Select(envelope => envelope.Message?.ContentEventOne).ShouldBe(["1", "2", "3", "4", "5"], ignoreOrder: true);
        eventTwoEnvelopes.Length.ShouldBe(5);
        eventTwoEnvelopes.Select(envelope => envelope.Endpoint.RawName).ShouldAllBe(rawName => rawName == "topic2");
        eventTwoEnvelopes.Select(envelope => envelope.Message?.ContentEventTwo).ShouldBe(["1", "2", "3", "4", "5"], ignoreOrder: true);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenSingleConsumersForMultipleTopicsIsConfigured()
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1"))
                                .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer1 = Helper.GetProducerForEndpoint("topic1");
        IProducer producer2 = Helper.GetProducerForEndpoint("topic2");

        for (int i = 1; i <= 5; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(10);

        IInboundEnvelope<TestEventOne>[] eventOneEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        IInboundEnvelope<TestEventTwo>[] eventTwoEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>().ToArray();

        eventOneEnvelopes.Length.ShouldBe(5);
        eventOneEnvelopes.Select(envelope => envelope.Endpoint.RawName).ShouldAllBe(rawName => rawName == "topic1");
        eventOneEnvelopes.Select(envelope => envelope.Message?.ContentEventOne).ShouldBe(
            ["1", "2", "3", "4", "5"],
            ignoreOrder: true);
        eventTwoEnvelopes.Length.ShouldBe(5);
        eventTwoEnvelopes.Select(envelope => envelope.Endpoint.RawName).ShouldAllBe(rawName => rawName == "topic2");
        eventTwoEnvelopes.Select(envelope => envelope.Message?.ContentEventTwo).ShouldBe(
            ["1", "2", "3", "4", "5"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeOnce_WhenMultipleConsumersForSameTopicAndWithSameConsumerGroupAreConfigured()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(4)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 8; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        KafkaConsumer[] consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().ToArray();
        consumers.Length.ShouldBe(2);
        consumers[0].Client.Assignment.Count.ShouldBe(2);
        consumers[1].Client.Assignment.Count.ShouldBe(2);

        IInboundEnvelope<TestEventOne>[] inboundEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        inboundEnvelopes.Select(envelope => envelope.Message?.ContentEventOne).ShouldBe(
            ["1", "2", "3", "4", "5", "6", "7", "8"],
            ignoreOrder: true);
        IInboundEnvelope<TestEventOne>[] consumer1Envelopes = inboundEnvelopes.Where(envelope => envelope.Consumer == consumers[0]).ToArray();
        IInboundEnvelope<TestEventOne>[] consumer2Envelopes = inboundEnvelopes.Where(envelope => envelope.Consumer == consumers[1]).ToArray();

        consumer1Envelopes.Length.ShouldBe(4);
        consumer2Envelopes.Length.ShouldBe(4);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeTwice_WhenMultipleConsumersForSameTopicAndWithDifferentConsumerGroupAreConfigured()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(4)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group1")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group2")
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 8; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        KafkaConsumer[] consumers = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().ToArray();
        consumers.Length.ShouldBe(2);
        consumers[0].Client.Assignment.Count.ShouldBe(4);
        consumers[1].Client.Assignment.Count.ShouldBe(4);

        IInboundEnvelope<TestEventOne>[] inboundEnvelopes = Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>().ToArray();
        inboundEnvelopes.Length.ShouldBe(16);
        IInboundEnvelope<TestEventOne>[] consumer1Envelopes = inboundEnvelopes.Where(envelope => envelope.Consumer == consumers[0]).ToArray();
        IInboundEnvelope<TestEventOne>[] consumer2Envelopes = inboundEnvelopes.Where(envelope => envelope.Consumer == consumers[1]).ToArray();

        consumer1Envelopes.Select(envelope => envelope.Message?.ContentEventOne).ShouldBe(
            ["1", "2", "3", "4", "5", "6", "7", "8"],
            ignoreOrder: true);
        consumer2Envelopes.Select(envelope => envelope.Message?.ContentEventOne).ShouldBe(
            ["1", "2", "3", "4", "5", "6", "7", "8"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldCommitOffsets_WhenAutoCommitIsEnabled()
    {
        TestKafkaOffsetCommittedCallback offsetCommittedCallback = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .EnableAutoCommit()
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerClientCallback(offsetCommittedCallback)
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value.ShouldBe(3);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 1))!.Offset.Value.ShouldBe(2);

        await AsyncTestingUtil.WaitAsync(() => offsetCommittedCallback.Offsets.Count == 2);

        offsetCommittedCallback.Offsets[new TopicPartition(DefaultTopicName, 0)].Value.ShouldBe(3);
        offsetCommittedCallback.Offsets[new TopicPartition(DefaultTopicName, 1)].Value.ShouldBe(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldCommitOffsets_WhenAutoCommitIsDisabled()
    {
        TestKafkaOffsetCommittedCallback offsetCommittedCallback = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(3)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerClientCallback(offsetCommittedCallback)
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "0" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(6);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value.ShouldBe(4);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 1))!.Offset.Value.ShouldBe(2);

        await AsyncTestingUtil.WaitAsync(() => offsetCommittedCallback.CallsCount >= 2);

        offsetCommittedCallback.CallsCount.ShouldBe(2);
        offsetCommittedCallback.Offsets[new TopicPartition(DefaultTopicName, 0)].Value.ShouldBe(4);
        offsetCommittedCallback.Offsets[new TopicPartition(DefaultTopicName, 1)].Value.ShouldBe(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldCommitOffsetsWhenDisconnecting_WhenAutoCommitIsDisabled()
    {
        int receivedMessages = 0;
        TestKafkaOffsetCommittedCallback offsetCommittedCallback = new();

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
                                .CommitOffsetEach(10)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonBrokerClientCallback(offsetCommittedCallback)
                .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => receivedMessages++;
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await AsyncTestingUtil.WaitAsync(() => receivedMessages == 3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        offsetCommittedCallback.CallsCount.ShouldBe(0);

        await Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single().Client.DisconnectAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
        offsetCommittedCallback.CallsCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeAfterStopAndStart()
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
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        await consumer.StopAsync();

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Task.Delay(100);

        await consumer.StartAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeAfterDisconnectAndReconnect()
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
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        await consumer.Client.DisconnectAsync();

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Task.Delay(100);

        await consumer.Client.ConnectAsync();

        await Helper.WaitUntilConnectedAsync();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldLimitParallelism()
    {
        List<TestEventWithKafkaKey> receivedMessages = [];
        TaskCompletionSource<bool> taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))
                                .LimitParallelism(2)))
                .AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage));

        async Task HandleMessage(TestEventWithKafkaKey message)
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(message);
            }

            await taskCompletionSource.Task;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 4, Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
        await Task.Delay(100);

        try
        {
            receivedMessages.Count.ShouldBe(2);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(12);
    }
}
