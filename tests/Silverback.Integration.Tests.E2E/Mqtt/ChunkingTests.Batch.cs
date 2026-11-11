// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ChunkingTests
{
    [Fact]
    public async Task Chunking_ShouldRejectChunksAndApplyErrorPolicy_WhenConsumedInBatch()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        TestingCollection<(IRawInboundEnvelope Envelope, Exception Exception)> skippedMessages = [];
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(callInfo =>
            {
                skippedMessages.Add((callInfo.ArgAt<IRawInboundEnvelope>(0), callInfo.ArgAt<Exception>(1)));
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = SerializeAndSplitIntoChunks("Long message", 3);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });

        // A message fitting in a single chunk doesn't start a chunk sequence and is added to the batch
        await producer.RawProduceAsync(SerializeAndSplitIntoChunks("2", 1).Single(), GetRawChunkHeaders("single", 0, 1));

        for (int i = 0; i < chunks.Length; i++)
        {
            await producer.RawProduceAsync(chunks[i], GetRawChunkHeaders("chunked", i, chunks.Length));
        }

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3"]);

        // Each chunk is rejected and skipped, and acknowledged while the batch is pending
        skippedMessages.Select(skipped => skipped.Envelope.Headers.GetValue(DefaultMessageHeaders.ChunkIndex)).ShouldBe(["0", "1", "2"]);
        skippedMessages.ShouldAllBe(skipped => skipped.Exception is SequenceException);
        skippedMessages[0].Exception.Message.ShouldContain("batch processing");
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Chunking_ShouldStopConsumerWithoutAcknowledging_WhenChunkIsConsumedInBatch()
    {
        TestingCollection<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            await foreach (TestEventOne message in batch)
            {
                receivedMessages.Add(message.ContentEventOne);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = SerializeAndSplitIntoChunks("Long message", 2);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.RawProduceAsync(chunks[0], GetRawChunkHeaders("chunked", 0, chunks.Length));

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(["1"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);
    }

    private static byte[][] SerializeAndSplitIntoChunks(string content, int chunksCount)
    {
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = content });
        int chunkSize = (rawMessage.Length + chunksCount - 1) / chunksCount;

        return [.. rawMessage.Chunk(chunkSize)];
    }

    private static MessageHeader[] GetRawChunkHeaders(string chunkMessageId, int chunkIndex, int chunksCount) =>
    [
        new(MqttMessageHeaders.ChunkMessageId, chunkMessageId),
        new(DefaultMessageHeaders.ChunkIndex, chunkIndex.ToString(CultureInfo.InvariantCulture)),
        new(DefaultMessageHeaders.ChunksCount, chunksCount.ToString(CultureInfo.InvariantCulture))
    ];
}
