// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingFixture
{
    [Fact]
    public async Task Chunking_ShouldDiscardIncompleteJson_WhenNextSequenceStarts()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { ContentEventOne = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 2, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldDiscardIncompleteJson_WhenNoSequenceMessageReceived()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { ContentEventOne = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2,
            HeadersHelper.GetHeaders("6", typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task Chunking_ShouldIgnoreErrorPolicy_WhenDiscardingIncompleteJson()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { ContentEventOne = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(5))))) // Defined retry policy will be ignored
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 2, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldDiscardIncompleteJsonAndConsumeNextMessage_WhenSameMessageIsSentAgain()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(
            () => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) > 0,
            TimeSpan.FromMilliseconds(200));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(0);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldDiscardIncompleteJsonAfterTimeout()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .WithSequenceTimeout(TimeSpan.FromMilliseconds(500)))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 1);

        await Task.Delay(200);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

        await Task.Delay(300);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await Task.Delay(500);
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.CommittedOffsets.Count > 0);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, 3, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Hello E2E!");
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldDiscardJsonMissingFirstChunkAndConsumeNextMessage()
    {
        TestEventOne message1 = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { ContentEventOne = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWithIncompleteJson()
    {
        TestEventOne message = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(5).Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 3);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await consumer.Client.DisconnectAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenRebalancingWithIncompleteJson()
    {
        TestEventOne message = new() { ContentEventOne = "Message 1" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(5).Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 3);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(3);

        await DefaultConsumerGroup.RebalanceAsync();
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(0);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await producer.RawProduceAsync(
            rawMessage.Skip(15).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 3, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(7);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);
    }
}
