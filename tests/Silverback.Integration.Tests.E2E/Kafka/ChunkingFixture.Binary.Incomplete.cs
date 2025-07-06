// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
    public async Task Chunking_ShouldDiscardIncompleteBinaryMessage_WhenNextSequenceStarts()
    {
        byte[] rawMessage1 = BytesUtil.GetRandomBytes(30);
        byte[] rawMessage2 = BytesUtil.GetRandomBytes(27);
        List<byte[]?> receivedFiles = [];
        int aborted = 0;

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
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage)
        {
            try
            {
                receivedFiles.Add(binaryMessage.Content.ReadAll());
            }
            catch (OperationCanceledException)
            {
                aborted++;
                throw;
            }
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
            HeadersHelper.GetChunkHeaders("6", 0));
        await producer.RawProduceAsync(
            rawMessage2.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 1));
        await producer.RawProduceAsync(
            rawMessage2.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("6", 2, true));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        receivedFiles.Count.ShouldBe(1);
        receivedFiles[0].ShouldBe(rawMessage2);
        aborted.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldDiscardIncompleteBinaryMessageAfterTimeout()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();
        List<byte[]?> receivedFiles = [];
        bool aborted = false;

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
                                .Consume<BinaryMessage>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .WithSequenceTimeout(TimeSpan.FromMilliseconds(500)))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage)
        {
            try
            {
                receivedFiles.Add(binaryMessage.Content.ReadAll());
            }
            catch (OperationCanceledException)
            {
                aborted = true;
                throw;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 1);

        await Task.Delay(200);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1));

        await AsyncTestingUtil.WaitAsync(() => aborted && DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 2);
        aborted.ShouldBeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);

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

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        receivedFiles.Count.ShouldBe(1);
        receivedFiles[0].ShouldBe(rawMessage);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldDiscardBinaryMessageMissingFirstChunkAndConsumeNextMessage()
    {
        byte[] rawMessage1 = BytesUtil.GetRandomBytes();
        byte[] rawMessage2 = BytesUtil.GetRandomBytes();

        List<byte[]?> receivedFiles = [];

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
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

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

        receivedFiles.Count.ShouldBe(1);
        receivedFiles[0].ShouldBe(rawMessage2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenDisconnectingWithIncompleteBinaryMessage()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes(50);
        bool enumerationAborted = false;

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
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        async ValueTask HandleMessage(BinaryMessage binaryMessage)
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
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 2);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await consumer.Client.DisconnectAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => enumerationAborted);

        enumerationAborted.ShouldBeTrue();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldAbortAndNotCommit_WhenRebalancingWithIncompleteBinaryMessage()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes();
        int aborted = 0;

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
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
                .AddIntegrationSpy());

        async ValueTask HandleMessage(BinaryMessage binaryMessage)
        {
            try
            {
                await binaryMessage.Content.ReadAllAsync();
            }
            catch (OperationCanceledException)
            {
                aborted++;
                throw;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            rawMessage.Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, 3));
        await producer.RawProduceAsync(
            rawMessage.Skip(5).Take(5).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, 3));

        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count >= 2);
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(2);

        await DefaultConsumerGroup.RebalanceAsync();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        aborted.ShouldBe(1);

        await producer.RawProduceAsync(
            rawMessage.Skip(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, 3));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(5);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
        aborted.ShouldBe(1);
    }
}
