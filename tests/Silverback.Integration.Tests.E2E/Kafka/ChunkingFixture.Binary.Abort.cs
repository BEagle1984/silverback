// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingFixture
{
    [Fact]
    [SuppressMessage("ReSharper", "MustUseReturnValue", Justification = "Test method")]
    public async Task Chunking_ShouldCommitAndConsumeNextMessage_WhenBinaryMessageReadAborted()
    {
        BinaryMessage message1 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "application/pdf" };
        BinaryMessage message2 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "text/plain" };
        byte[] rawMessage1 = message1.Content.ReadAll()!;
        byte[] rawMessage2 = message2.Content.ReadAll()!;
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

        void HandleMessage(BinaryMessage binaryMessage)
        {
            if (binaryMessage.ContentType != "text/plain")
            {
                // Read first chunk only
                byte[] buffer = new byte[10];
                binaryMessage.Content!.Read(buffer, 0, 10);
                return;
            }

            receivedFiles.Add(binaryMessage.Content.ReadAll());
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, contentType: message1.ContentType));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, contentType: message1.ContentType));
        await producer.RawProduceAsync(
            rawMessage1.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, contentType: message1.ContentType));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, contentType: message2.ContentType));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, contentType: message2.ContentType));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, contentType: message2.ContentType));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    [SuppressMessage("ReSharper", "MustUseReturnValue", Justification = "Test method")]
    public async Task Chunking_ShouldCommitAndConsumeNextMessage_WhenBinaryMessageReadAbortedMidChunk()
    {
        BinaryMessage message1 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "application/pdf" };
        BinaryMessage message2 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "text/plain" };
        byte[] rawMessage1 = message1.Content.ReadAll()!;
        byte[] rawMessage2 = message2.Content.ReadAll()!;
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

        void HandleMessage(BinaryMessage binaryMessage)
        {
            if (binaryMessage.ContentType != "text/plain")
            {
                // Read only part of first chunk
                byte[] buffer = new byte[5];
                binaryMessage.Content!.Read(buffer, 0, 5);
                return;
            }

            receivedFiles.Add(binaryMessage.Content.ReadAll());
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, contentType: message1.ContentType));
        await producer.RawProduceAsync(
            rawMessage1.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, contentType: message1.ContentType));
        await producer.RawProduceAsync(
            rawMessage1.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, contentType: message1.ContentType));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, contentType: message2.ContentType));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, contentType: message2.ContentType));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, contentType: message2.ContentType));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    [SuppressMessage("ReSharper", "MustUseReturnValue", Justification = "Test method")]
    public async Task Chunking_ShouldCommitAndConsumeNextMessage_WhenSingleChunkBinaryMessageReadAborted()
    {
        BinaryMessage message1 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "application/pdf" };
        BinaryMessage message2 = new() { Content = BytesUtil.GetRandomStream(30), ContentType = "text/plain" };
        byte[] rawMessage1 = message1.Content.ReadAll()!;
        byte[] rawMessage2 = message2.Content.ReadAll()!;
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

        void HandleMessage(BinaryMessage binaryMessage)
        {
            if (binaryMessage.ContentType != "text/plain")
            {
                // Partially read first chunk only
                byte[] buffer = new byte[10];
                binaryMessage.Content!.Read(buffer, 0, 10);
                return;
            }

            receivedFiles.Add(binaryMessage.Content.ReadAll());
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage1.ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, true, contentType: message1.ContentType));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

        await producer.RawProduceAsync(
            rawMessage2.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, contentType: message2.ContentType));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, contentType: message2.ContentType));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, contentType: message2.ContentType));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.InboundEnvelopes[0].Message.As<BinaryMessage>().ContentType.Should().Be("application/pdf");
        Helper.Spy.InboundEnvelopes[1].Message.As<BinaryMessage>().ContentType.Should().Be("text/plain");

        receivedFiles.Should().HaveCount(1);
        receivedFiles[0].Should().BeEquivalentTo(message2.Content.ReReadAll());

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(4);
    }
}
