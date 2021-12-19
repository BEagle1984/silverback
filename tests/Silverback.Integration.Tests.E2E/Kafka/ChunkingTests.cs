// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ChunkingTests : KafkaTestFixture
{
    public ChunkingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Chunking_Json_ProducedAndConsumed()
    {
        const int chunksPerMessage = 3;

        Host.ConfigureServices(
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
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(5 * chunksPerMessage);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        for (int i = 0; i < Helper.Spy.RawOutboundEnvelopes.Count; i++)
        {
            int firstEnvelopeIndex = i / chunksPerMessage * chunksPerMessage;
            IRawOutboundEnvelope firstEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex];
            IRawOutboundEnvelope lastEnvelope = Helper.Spy.RawOutboundEnvelopes[firstEnvelopeIndex + chunksPerMessage - 1];
            IRawOutboundEnvelope envelope = Helper.Spy.RawOutboundEnvelopes[i];

            envelope.Headers.GetValue(DefaultMessageHeaders.ChunksCount).Should()
                .Be(chunksPerMessage.ToString(CultureInfo.InvariantCulture));

            if (envelope == firstEnvelope)
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should().BeNull();
            }
            else
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.FirstChunkOffset).Should()
                    .Be(firstEnvelope.BrokerMessageIdentifier!.Value);
            }

            if (envelope == lastEnvelope)
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).Should().Be(true.ToString());
            }
            else
            {
                envelope.Headers.GetValue(DefaultMessageHeaders.IsLastChunk).Should().BeNull();
            }
        }

        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 5).Select(i => $"Long message {i}"));
    }

    [Fact]
    public async Task Chunking_BinaryMessage_ProducedAndConsumed()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(25),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            receivedFiles.Add(binaryMessage.Content.ReadAll());
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.RawOutboundEnvelopes.ForEach(envelope => envelope.RawMessage.ReReadAll()!.Length.Should().BeLessOrEqualTo(10));
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeOfType<BinaryMessage>());

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(new[] { message1.Content.ReReadAll(), message2.Content.ReReadAll() });
    }

    [Fact]
    public async Task Chunking_JsonWithIsLastChunkHeader_Consumed()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, false, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_JsonWithChunksCountHeader_Consumed()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_JsonWithMessageIdHeader_Consumed()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageWithIsLastChunkHeader_Consumed()
    {
        byte[] rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound<BinaryMessage>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                            receivedFiles.Add(binaryMessage.Content.ReadAll())))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            rawMessage.Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, false));
        await producer.RawProduceAsync(
            rawMessage.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1));
        await producer.RawProduceAsync(
            rawMessage.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(rawMessage);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageWithChunksCountHeader_Consumed()
    {
        byte[] rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound<BinaryMessage>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                            receivedFiles.Add(binaryMessage.Content.ReadAll())))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            rawMessage.Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(rawMessage);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageWithMessageIdHeader_Consumed()
    {
        byte[] rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound<BinaryMessage>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                            receivedFiles.Add(binaryMessage.Content.ReadAll())))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            rawMessage.Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(rawMessage);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageWithMessageTypeHeader_Consumed()
    {
        byte[] rawMessage = Encoding.UTF8.GetBytes("Hello E2E!");
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                            receivedFiles.Add(binaryMessage.Content.ReadAll())))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
        await producer.RawProduceAsync(
            rawMessage.Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(BinaryMessage)));
        await producer.RawProduceAsync(
            rawMessage.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(BinaryMessage)));
        await producer.RawProduceAsync(
            rawMessage.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(BinaryMessage)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(rawMessage);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_JsonWithDuplicatedChunks_DuplicatesIgnored()
    {
        TestEventOne message1 = new() { Content = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { Content = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<TestEventOne>().Content)
            .Should().BeEquivalentTo("Message 1", "Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageWithDuplicatedChunks_DuplicatesIgnored()
    {
        byte[] rawMessage1 = Encoding.UTF8.GetBytes("Message 1");
        byte[] rawMessage2 = Encoding.UTF8.GetBytes("Message 2");
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound<BinaryMessage>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                            receivedFiles.Add(binaryMessage.Content.ReadAll())))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage1.Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("6", 0));
        await producer.RawProduceAsync(
            rawMessage1.Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("6", 0));
        await producer.RawProduceAsync(
            rawMessage1.Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("6", 0));
        await producer.RawProduceAsync(
            rawMessage1.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("6", 1));
        await producer.RawProduceAsync(
            rawMessage1.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeadersWithMessageId("6", 2, true));

        await producer.RawProduceAsync(
            rawMessage2.Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage2.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));
        await producer.RawProduceAsync(
            rawMessage2.Skip(3).Take(3).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));
        await producer.RawProduceAsync(
            rawMessage2.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3));
        await producer.RawProduceAsync(
            rawMessage2.Skip(6).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(new[] { rawMessage1, rawMessage2 });

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageReadAborted_CommittedAndNextMessageConsumed()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            if (binaryMessage.ContentType != "text/plain")
                            {
                                // Read first chunk only
                                byte[] buffer = new byte[10];
                                binaryMessage.Content!.Read(buffer, 0, 10);
                                return;
                            }

                            receivedFiles.Add(binaryMessage.Content.ReadAll());
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task Chunking_BinaryMessageReadAbortedMidChunk_CommittedAndNextMessageConsumed()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            if (binaryMessage.ContentType != "text/plain")
                            {
                                // Read only part of first chunk
                                byte[] buffer = new byte[5];
                                binaryMessage.Content!.Read(buffer, 0, 5);
                                return;
                            }

                            receivedFiles.Add(binaryMessage.Content.ReadAll());
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should()
            .Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should()
            .Be("text/plain");

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task Chunking_BinaryMessageProcessingFailedAfterFirstChunk_DisconnectedAndNotCommitted()
    {
        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            // Read first chunk only
                            byte[] buffer = new byte[10];
                            binaryMessage.Content!.Read(buffer, 0, 10);

                            throw new InvalidOperationException("Test");
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
            new BinaryMessage
            {
                Content = BytesUtil.GetRandomStream(),
                ContentType = "application/pdf"
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageProcessingFailedImmediately_DisconnectedAndNotCommitted()
    {
        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber((BinaryMessage _) => throw new InvalidOperationException("Test"))
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
            new BinaryMessage
            {
                Content = BytesUtil.GetRandomStream(),
                ContentType = "application/pdf"
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task
        Chunking_EnforcingConsecutiveJsonChunks_IncompleteSequenceDiscardedWhenOtherSequenceStarts()
    {
        TestEventOne message1 = new() { Content = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { Content = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task
        Chunking_EnforcingConsecutiveJsonChunks_IncompleteSequenceDiscardedOnNoSequenceMessage()
    {
        TestEventOne message1 = new() { Content = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { Content = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task
        Chunking_EnforcingConsecutiveJsonChunks_ErrorPolicyIgnoredWhenIncompleteSequenceDiscarded()
    {
        TestEventOne message1 = new() { Content = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { Content = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .OnError(policy => policy.Retry(5))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));
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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_IncompleteJsonResent_SecondMessageConsumedAndCommitted()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(
            () => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) > 0,
            TimeSpan.FromMilliseconds(200));
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? originalSequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        originalSequence.Should().NotBeNull();
        originalSequence!.IsComplete.Should().BeFalse();
        originalSequence.IsAborted.Should().BeFalse();

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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        originalSequence.IsAborted.Should().BeTrue();
        sequenceStore.GetPendingSequences().Should().BeEmpty();
    }

    [Fact]
    public async Task Chunking_IncompleteJson_AbortedAfterTimeoutAndCommitted()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .WithSequenceTimeout(TimeSpan.FromMilliseconds(500))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(
            () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                  Helper.Broker.Consumers[0].SequenceStores.Single().GetPendingSequences().Any());

        await Task.Delay(200);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        sequence.Should().NotBeNull();
        sequence!.IsAborted.Should().BeFalse();

        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));

        await Task.Delay(300);
        sequence.IsAborted.Should().BeFalse();

        await AsyncTestingUtil.WaitAsync(
            () => !sequence.IsPending && DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 2,
            TimeSpan.FromSeconds(1));
        sequence.IsAborted.Should().BeTrue();

        sequenceStore.GetPendingSequences().Should().BeEmpty();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);

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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Hello E2E!");
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        sequenceStore.GetPendingSequences().Should().BeEmpty();
    }

    [Fact]
    public async Task Chunking_IncompleteBinaryMessage_AbortedAfterTimeoutAndCommitted()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();
        bool enumerationAborted = false;
        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConsumeBinaryMessages()
                                    .WithSequenceTimeout(TimeSpan.FromMilliseconds(500))
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            try
                            {
                                receivedFiles.Add(binaryMessage.Content.ReadAll());
                            }
                            catch (OperationCanceledException)
                            {
                                enumerationAborted = true;
                                throw;
                            }
                        })
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0));

        await AsyncTestingUtil.WaitAsync(
            () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                  Helper.Broker.Consumers[0].SequenceStores.Single().GetPendingSequences().Any());

        await Task.Delay(200);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        sequence.Should().NotBeNull();
        sequence!.IsAborted.Should().BeFalse();

        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1));

        await Task.Delay(300);
        sequence.IsAborted.Should().BeFalse();

        await AsyncTestingUtil.WaitAsync(() => enumerationAborted);
        sequence.IsAborted.Should().BeTrue();
        sequenceStore.GetPendingSequences().Should().BeEmpty();
        enumerationAborted.Should().BeTrue();

        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, true));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(rawMessage);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        sequenceStore.GetPendingSequences().Should().BeEmpty();
    }

    [Fact]
    public async Task Chunking_JsonMissingFirstChunk_NextMessageConsumedAndCommitted()
    {
        TestEventOne message1 = new() { Content = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { Content = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

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

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 2");

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_BinaryMessageMissingFirstChunk_NextMessageConsumedAndCommitted()
    {
        byte[] rawMessage1 = BytesUtil.GetRandomBytes();
        byte[] rawMessage2 = BytesUtil.GetRandomBytes();

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                            receivedFiles.Add(binaryMessage.Content.ReadAll())))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(BinaryMessage)));
        await producer.RawProduceAsync(
            rawMessage1.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(BinaryMessage)));
        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0, typeof(BinaryMessage)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1, typeof(BinaryMessage)));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, true, typeof(BinaryMessage)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(rawMessage2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        sequenceStores.Any(store => store.GetPendingSequences().Any()).Should().BeFalse();
    }

    [Fact]
    public async Task Chunking_DisconnectWithIncompleteJson_AbortedAndNotCommitted()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(
            async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                        Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                        (await Helper.Broker.Consumers[0].SequenceStores.Single()
                            .GetAsync<ChunkSequence>("1"))?.Length == 2);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        sequence.Should().NotBeNull();
        sequence!.Length.Should().Be(2);
        sequence.IsAborted.Should().BeFalse();

        await Helper.Broker.DisconnectAsync();

        sequence.IsAborted.Should().BeTrue();
        sequenceStore.GetPendingSequences().Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Chunking_DisconnectWithIncompleteBinaryMessage_AbortedAndNotCommitted()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();
        bool enumerationAborted = false;

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConsumeBinaryMessages()
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (BinaryMessage binaryMessage) =>
                        {
                            try
                            {
                                await binaryMessage.Content.ReadAllAsync();
                            }
                            catch (OperationCanceledException)
                            {
                                enumerationAborted = true;
                                throw;
                            }
                        })
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));

        await AsyncTestingUtil.WaitAsync(
            async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                        Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                        (await Helper.Broker.Consumers[0].SequenceStores.Single()
                            .GetAsync<ChunkSequence>("1"))?.Length == 2);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        sequence.Should().NotBeNull();
        sequence!.Length.Should().Be(2);
        sequence.IsAborted.Should().BeFalse();

        await Helper.Broker.DisconnectAsync();

        sequence.IsAborted.Should().BeTrue();
        sequenceStore.GetPendingSequences().Should().BeEmpty();
        enumerationAborted.Should().BeTrue();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Chunking_RebalanceWithIncompleteJson_AbortedAndNotCommitted()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(
            async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                        Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                        (await Helper.Broker.Consumers[0].SequenceStores.Single()
                            .GetAsync<ChunkSequence>("1"))?.Length == 2);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        sequence.Should().NotBeNull();
        sequence!.Length.Should().Be(2);
        sequence.IsAborted.Should().BeFalse();

        DefaultConsumerGroup.Rebalance();

        await AsyncTestingUtil.WaitAsync(() => sequence.IsAborted);

        sequence.IsAborted.Should().BeTrue();
        sequenceStore.GetPendingSequences().Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Chunking_RebalanceWithIncompleteBinaryMessage_AbortedAndNotCommitted()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();
        bool enumerationAborted = false;

        Host.ConfigureServices(
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
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConsumeBinaryMessages()
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (BinaryMessage binaryMessage) =>
                        {
                            try
                            {
                                await binaryMessage.Content.ReadAllAsync();
                            }
                            catch (OperationCanceledException)
                            {
                                enumerationAborted = true;
                                throw;
                            }
                        })
                    .AddIntegrationSpy())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));

        await AsyncTestingUtil.WaitAsync(
            async () => Helper.Broker.Consumers[0].SequenceStores.Count >= 1 &&
                        Helper.Broker.Consumers[0].SequenceStores.Single().Count == 1 &&
                        (await Helper.Broker.Consumers[0].SequenceStores.Single()
                            .GetAsync<ChunkSequence>("1"))?.Length == 2);

        ISequenceStore sequenceStore = Helper.Broker.Consumers[0].SequenceStores.Single();
        ChunkSequence? sequence = await sequenceStore.GetAsync<ChunkSequence>("1");
        sequence.Should().NotBeNull();
        sequence!.Length.Should().Be(2);
        sequence.IsAborted.Should().BeFalse();

        DefaultConsumerGroup.Rebalance();

        await AsyncTestingUtil.WaitAsync(() => sequence.IsAborted && enumerationAborted);

        sequence.IsAborted.Should().BeTrue();
        sequenceStore.GetPendingSequences().Should().BeEmpty();
        enumerationAborted.Should().BeTrue();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Chunking_JsonFromMultiplePartitions_ConcurrentlyConsumed()
    {
        const int messagesCount = 10;
        const int chunksPerMessage = 3;

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= messagesCount; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"Long message {i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(messagesCount);
        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(messagesCount * chunksPerMessage);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(messagesCount);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, messagesCount).Select(i => $"Long message {i}"));
    }

    [Fact]
    public async Task Chunking_BinaryMessagesFromMultiplePartitions_ConcurrentlyConsumed()
    {
        byte[] rawMessage1 = BytesUtil.GetRandomBytes(30);
        byte[] rawMessage2 = BytesUtil.GetRandomBytes(30);
        byte[] rawMessage3 = BytesUtil.GetRandomBytes(30);

        int receivedFilesCount = 0;
        List<byte[]?> receivedFiles = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConsumeBinaryMessages()
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            Interlocked.Increment(ref receivedFilesCount);

                            byte[]? fileContent = binaryMessage.Content.ReadAll();

                            lock (receivedFiles)
                            {
                                receivedFiles.Add(fileContent);
                            }
                        }))
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));
        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1));
        await producer.RawProduceAsync(
            rawMessage3.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("3", 0));
        await producer.RawProduceAsync(
            rawMessage3.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("3", 1));

        await AsyncTestingUtil.WaitAsync(() => receivedFilesCount == 3);

        receivedFilesCount.Should().Be(3);
        receivedFiles.Should().BeEmpty();

        await producer.RawProduceAsync(
            rawMessage3.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("3", 2, true));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, true));
        await producer.RawProduceAsync(
            rawMessage1.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFilesCount.Should().Be(3);
        receivedFiles.Should().HaveCount(3);
        receivedFiles.Should().BeEquivalentTo(new[] { rawMessage1, rawMessage2, rawMessage3 });
    }

    [Fact]
    public async Task Chunking_SingleChunkJson_ProducedAndConsumed()
    {
        TestEventOne message1 = new() { Content = "Message 1" };
        byte[] rawMessage1 = DefaultSerializers.Json.SerializeToBytes(message1);
        TestEventOne message2 = new() { Content = "Message 2" };
        byte[] rawMessage2 = DefaultSerializers.Json.SerializeToBytes(message2);

        Host.ConfigureServices(
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
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(50))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e")));

        await producer.RawProduceAsync(
            rawMessage1.ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage2.ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.As<TestEventOne>().Content.Should().Be("Message 1");
        Helper.Spy.InboundEnvelopes[1].Message.As<TestEventOne>().Content.Should().Be("Message 2");
    }

    [Fact]
    public async Task Chunking_SingleChunkBinaryMessage_ProducedAndConsumed()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(8),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(8),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            lock (receivedFiles)
                            {
                                receivedFiles.Add(binaryMessage.Content.ReadAll());
                            }
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFiles.Should().HaveCount(2);
        receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
        receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task Chunking_SingleChunkBinaryMessageReadAborted_CommittedAndNextMessageConsumed()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(8),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(30),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServices(
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
                            .AddOutbound<BinaryMessage>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .EnableChunking(10))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (BinaryMessage binaryMessage) =>
                        {
                            if (binaryMessage.ContentType != "text/plain")
                            {
                                // Read first chunk only
                                byte[] buffer = new byte[10];
                                binaryMessage.Content!.Read(buffer, 0, 10);
                                return;
                            }

                            receivedFiles.Add(binaryMessage.Content.ReadAll());
                        })
                    .AddIntegrationSpy())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(4);
    }
}
