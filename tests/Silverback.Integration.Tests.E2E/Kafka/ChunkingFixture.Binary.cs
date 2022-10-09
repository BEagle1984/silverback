﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingFixture
{
    [Fact]
    public async Task Chunking_ShouldProduceAndConsumeChunkedBinaryMessage()
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

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

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
    public async Task Chunking_ShouldConsumeChunkedBinaryMessageWithIsLastChunkHeader()
    {
        List<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            byte[] rawMessage = Encoding.UTF8.GetBytes($"Long message {i}");

            await producer.RawProduceAsync(
                rawMessage.Take(5).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, false));
            await producer.RawProduceAsync(
                rawMessage.Skip(5).Take(5).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1));
            await producer.RawProduceAsync(
                rawMessage.Skip(10).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, true));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFiles.Should().HaveCount(3);
        receivedFiles[0].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 1"));
        receivedFiles[1].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 2"));
        receivedFiles[2].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 3"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(9);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeChunkedBinaryMessageWithChunksCountHeader()
    {
        List<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            byte[] rawMessage = Encoding.UTF8.GetBytes($"Long message {i}");

            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeaders("1", 2, 3));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFiles.Should().HaveCount(3);
        receivedFiles[0].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 1"));
        receivedFiles[1].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 2"));
        receivedFiles[2].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 3"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(9);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeChunkedBinaryMessageWithMessageIdHeader()
    {
        List<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            byte[] rawMessage = Encoding.UTF8.GetBytes($"Long message {i}");

            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFiles.Should().HaveCount(3);
        receivedFiles[0].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 1"));
        receivedFiles[1].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 2"));
        receivedFiles[2].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 3"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(9);
    }

    [Fact]
    public async Task Chunking_ShouldConsumeChunkedBinaryMessageWithMessageTypeHeader()
    {
        List<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            byte[] rawMessage = Encoding.UTF8.GetBytes($"Long message {i}");

            await producer.RawProduceAsync(
                rawMessage.Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 0, 3, typeof(BinaryMessage)));
            await producer.RawProduceAsync(
                rawMessage.Skip(3).Take(3).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 1, 3, typeof(BinaryMessage)));
            await producer.RawProduceAsync(
                rawMessage.Skip(6).ToArray(),
                HeadersHelper.GetChunkHeadersWithMessageId("1", 2, 3, typeof(BinaryMessage)));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedFiles.Should().HaveCount(3);
        receivedFiles[0].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 1"));
        receivedFiles[1].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 2"));
        receivedFiles[2].Should().BeEquivalentTo(Encoding.UTF8.GetBytes("Long message 3"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(9);
    }

    [Fact]
    public async Task Chunking_ShouldIgnoreDuplicatedChunksInBinaryMessage()
    {
        byte[] rawMessage1 = Encoding.UTF8.GetBytes("Message 1");
        byte[] rawMessage2 = Encoding.UTF8.GetBytes("Message 2");
        List<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

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
    }

    [Fact]
    public async Task Chunking_ShouldConsumeBinaryMessagesConcurrently_WhenProducedToMultiplePartitions()
    {
        byte[] rawMessage1 = BytesUtil.GetRandomBytes(30);
        byte[] rawMessage2 = BytesUtil.GetRandomBytes(30);
        byte[] rawMessage3 = BytesUtil.GetRandomBytes(30);

        int receivedFilesCount = 0;
        TestingCollection<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpyAndSubscriber());

        void HandleMessage(BinaryMessage binaryMessage)
        {
            Interlocked.Increment(ref receivedFilesCount);
            receivedFiles.Add(binaryMessage.Content.ReadAll());
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

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
    public async Task Chunking_ShouldProduceAndConsumeSingleChunkBinaryMessage()
    {
        BinaryMessage message1 = new() { Content = BytesUtil.GetRandomStream(8), ContentType = "application/pdf" };
        BinaryMessage message2 = new() { Content = BytesUtil.GetRandomStream(8), ContentType = "text/plain" };
        TestingCollection<byte[]?> receivedFiles = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EnableChunking(10)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpyAndSubscriber());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        receivedFiles.Should().HaveCount(2);
        receivedFiles[0].Should().BeEquivalentTo(message1.Content.ReReadAll());
        receivedFiles[1].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }
}