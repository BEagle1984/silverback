// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingFixture
{
    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeChunkedJson()
    {
        const int chunkSize = 10;
        const int chunksPerMessage = 4;

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
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(5 * chunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.ShouldBeLessThanOrEqualTo(chunkSize));
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);

        for (int i = 0; i < Helper.Spy.RawOutboundEnvelopes.Count; i++)
        {
            int firstEnvelopeIndex = i / chunksPerMessage * chunksPerMessage;
            IOutboundEnvelope firstEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex];
            IOutboundEnvelope lastEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex + chunksPerMessage - 1];
            IOutboundEnvelope envelope = Helper.Spy.RawOutboundEnvelopes[i];

            envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex).ShouldBe((i % chunksPerMessage).ToString(CultureInfo.InvariantCulture));
            envelope.Headers.GetValue(DefaultMessageHeaders.ChunksCount).ShouldBe(chunksPerMessage.ToString(CultureInfo.InvariantCulture));

            if (envelope == firstEnvelope)
            {
                envelope.Headers.GetValue(KafkaMessageHeaders.FirstChunkOffset).ShouldBeNull();
            }
            else
            {
                envelope.Headers.GetValue(KafkaMessageHeaders.FirstChunkOffset).ShouldBe(((KafkaOffset)firstEnvelope.BrokerMessageIdentifier!).Offset.ToString());
            }

            if (envelope == lastEnvelope)
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).ShouldBe(true.ToString());
            }
            else
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).ShouldBeNull();
            }
        }

        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeChunkedJsonWithCustomHeaderNames()
    {
        const int chunkSize = 10;
        const int chunksPerMessage = 4;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .WithCustomHeaderName(DefaultMessageHeaders.ChunkIndex, "x-quack-index")
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(chunkSize)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(5 * chunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.ShouldBeLessThanOrEqualTo(chunkSize));
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(5);

        for (int i = 0; i < Helper.Spy.RawOutboundEnvelopes.Count; i++)
        {
            IOutboundEnvelope envelope = Helper.Spy.RawOutboundEnvelopes[i];

            envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex).ShouldBeNullOrEmpty();
            envelope.Headers.GetValue("x-quack-index").ShouldBe((i % chunksPerMessage).ToString(CultureInfo.InvariantCulture));
        }

        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
    }

    [Fact]
    public async Task Chunking_ShouldConsumeChunkedJsonWithIsLastChunkHeader()
    {
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

        for (int i = 1; i <= 3; i++)
        {
            TestEventOne message = new() { ContentEventOne = $"Long message {i}" };
            byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, false, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message 1");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message 2");
        Helper.Spy.InboundEnvelopes[2].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message 3");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(9);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeChunkedJsonWithChunksCountHeader()
    {
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

        for (int i = 1; i <= 3; i++)
        {
            TestEventOne message = new() { ContentEventOne = $"Long message {i}" };
            byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

            await producer.RawProduceAsync(
                rawMessage.Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).Take(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
            await producer.RawProduceAsync(
                rawMessage.Skip(20).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message 1");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message 2");
        Helper.Spy.InboundEnvelopes[2].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Long message 3");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(9);
    }

    [Fact]
    public async Task Chunking_ShouldIgnoreDuplicatedChunksInJson()
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
            HeadersHelper.GetChunkHeadersWithMessageId("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("2", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("2", 2, true, typeof(TestEventOne)));

        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.ShouldBeOfType<TestEventOne>().ContentEventOne)
            .ShouldBe(["Message 1", "Message 2"]);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeJsonMessagesConcurrently_WhenProducedToMultiplePartitions()
    {
        const int messagesCount = 10;
        const int chunksPerMessage = 5;

        int receivedMessagesCount = 0;
        using CancellationTokenSource cancellationTokenSource = new();

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
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<TestEventWithKafkaKey>(HandleMessage)
                .AddIntegrationSpyAndSubscriber());

        async ValueTask HandleMessage(TestEventWithKafkaKey message)
        {
            Interlocked.Increment(ref receivedMessagesCount);

            await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(5)), cancellationTokenSource.Token.AsTask());
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishEventAsync(new TestEventWithKafkaKey { Content = $"Long message {i}", KafkaKey = i });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessagesCount == 3);
        receivedMessagesCount.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(messagesCount);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(messagesCount * chunksPerMessage);

        cancellationTokenSource.Cancel();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(messagesCount);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventWithKafkaKey)envelope.Message!).Content)
            .ShouldBe(Enumerable.Range(1, messagesCount).Select(i => $"Long message {i}"), ignoreOrder: true);
    }

    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeSingleChunkJson()
    {
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
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(100)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Message 1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Message 2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes[0].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Message 1");
        Helper.Spy.InboundEnvelopes[1].Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }
}
