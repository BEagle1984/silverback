// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Chunking_ShouldApplyErrorPoliciesOnce_WhenChunkedMessageAddedToBatchFails(bool retryBeforeStopping)
    {
        int batchesCount = 0;
        int stopCalls = 0;
        int retryCalls = 0;

        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return Task.CompletedTask;
            });
        IRetryErrorPolicyCallback retryCallback = Substitute.For<IRetryErrorPolicyCallback>();
        retryCallback.OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref retryCalls);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .OnError(policy =>
                        {
                            if (retryBeforeStopping)
                                policy.Retry(1).ThenStop();
                            else
                                policy.Stop();
                        }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(stopCallback)
            .AddSingletonErrorPolicyCallback(retryCallback)
            .AddIntegrationSpy());

        // The subscriber fails processing the chunked message added to the batch started by the previous message
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            await foreach (TestEventOne message in batch)
            {
                if (message.ContentEventOne == "Long message")
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = ChunksHelper.SerializeAndSplitIntoChunks("Long message", 3);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Short message" });

        for (int i = 0; i < chunks.Length; i++)
        {
            await producer.RawProduceAsync(chunks[i], HeadersHelper.GetChunkHeaders("long", i, chunks.Length, typeof(TestEventOne)));
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        // Give a second application of the error policies the time to happen
        await Task.Delay(500);

        batchesCount.ShouldBe(retryBeforeStopping ? 2 : 1);
        retryCalls.ShouldBe(retryBeforeStopping ? 1 : 0);
        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Chunking_ShouldStopConsumerAndDisconnect_WhenBatchOfChunkedMessagesFails(bool retryBeforeStopping, bool throwImmediately)
    {
        int batchesCount = 0;
        int stopCalls = 0;

        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(10)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2)
                        .OnError(policy =>
                        {
                            if (retryBeforeStopping)
                                policy.Retry(2);
                            else
                                policy.Stop();
                        }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(stopCallback)
            .AddIntegrationSpy());

        // The batch is started by a chunked message, and ended by one (unless the subscriber fails before it's added), whose reader awaits
        // the processing of the chunk sequence instead of the batch
        async ValueTask HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            if (!throwImmediately)
            {
                await foreach (TestEventOne dummy in batch)
                {
                    // Enumerate the whole batch before failing
                }
            }

            throw new InvalidOperationException("Test");
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 2" });

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        // Give a second application of the error policies the time to happen
        await Task.Delay(500);

        batchesCount.ShouldBe(retryBeforeStopping ? 3 : 1);
        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Chunking_ShouldStopConsumerAndDisconnect_WhenDeserializationFailsWhileJsonConsumedInBatch(bool retryBeforeStopping)
    {
        TestingCollection<List<string?>> batches = [];
        int stopCalls = 0;
        int retryCalls = 0;

        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return Task.CompletedTask;
            });
        IRetryErrorPolicyCallback retryCallback = Substitute.For<IRetryErrorPolicyCallback>();
        retryCallback.OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref retryCalls);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .OnError(policy =>
                        {
                            if (retryBeforeStopping)
                                policy.Retry(1).ThenStop();
                            else
                                policy.Stop();
                        }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(stopCallback)
            .AddSingletonErrorPolicyCallback(retryCallback)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            batches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = ChunksHelper.SerializeAndSplitIntoChunks("Long message A", 3);
        byte[][] invalidChunks = [.. "{ invalid json"u8.ToArray().Chunk(7)];

        for (int i = 0; i < chunks.Length; i++)
        {
            await producer.RawProduceAsync(chunks[i], HeadersHelper.GetChunkHeaders("A", i, chunks.Length, typeof(TestEventOne)));
        }

        // The second chunked message can't be deserialized while the batch is pending
        for (int i = 0; i < invalidChunks.Length; i++)
        {
            await producer.RawProduceAsync(
                invalidChunks[i],
                HeadersHelper.GetChunkHeaders("B", i, invalidChunks.Length, typeof(TestEventOne)));
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        batches.Count.ShouldBe(retryBeforeStopping ? 2 : 1);
        batches.ShouldAllBe(batch => batch.Count == 1 && batch[0] == "Long message A");
        retryCalls.ShouldBe(retryBeforeStopping ? 1 : 0);
        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("move")]
    [InlineData("retry")]
    [InlineData("skip")]
    public async Task Chunking_ShouldApplyErrorPolicy_WhenProcessingFailsWhileBinaryMessageConsumedInBatch(string policy)
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<BinaryMessage>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .OnError(errorPolicy =>
                        {
                            switch (policy)
                            {
                                case "move":
                                    errorPolicy.MoveTo("other-topic");
                                    break;
                                case "retry":
                                    errorPolicy.Retry(1);
                                    break;
                                case "skip":
                                    errorPolicy.Skip();
                                    break;
                                default:
                                    errorPolicy.Stop();
                                    break;
                            }
                        }))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            await foreach (BinaryMessage message in batch)
            {
                // Fail the first batch while the second binary message is partly read: its first chunk is still being pushed and its
                // next chunks are pending
                if (isFirstBatch && list.Count == 1)
                {
                    byte[] buffer = new byte[5];
                    await message.Content!.ReadExactlyAsync(buffer);
                    throw new InvalidOperationException("Test");
                }

                byte[]? readAll = await message.Content.ReadAllAsync();
                list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks1 = [.. "Long message 1"u8.ToArray().Chunk(5)];
        byte[][] chunks2 = [.. "Long message 2"u8.ToArray().Chunk(5)];

        for (int i = 0; i < chunks1.Length; i++)
        {
            await producer.RawProduceAsync(chunks1[i], HeadersHelper.GetChunkHeaders("1", i, chunks1.Length));
        }

        for (int i = 0; i < chunks2.Length; i++)
        {
            await producer.RawProduceAsync(chunks2[i], HeadersHelper.GetChunkHeaders("2", i, chunks2.Length));
        }

        for (int i = 3; i <= 5; i++)
        {
            await producer.RawProduceAsync(Encoding.UTF8.GetBytes($"Message {i}"), HeadersHelper.GetChunkHeaders($"{i}", 0, 1));
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);

        switch (policy)
        {
            case "retry":
                // The failed batch is consumed again
                await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 7);

                batches.Count.ShouldBeGreaterThanOrEqualTo(2);
                batches[1].ShouldBe(["Long message 1", "Long message 2", "Message 3"]);
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
                consumer.StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
                logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
                break;
            case "skip":
                // The failed batch is skipped, with the remaining chunks of the binary message
                await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 9);

                batches.Count.ShouldBe(2);
                batches[1].ShouldBe(["Message 3", "Message 4", "Message 5"]);
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(9);
                consumer.StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
                logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
                break;
            default:
                // The move policy can't handle sequences: the consumer is stopped, as when no error policy handles the failure
                await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

                batchesCount.ShouldBe(1);
                consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
                consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
                Helper.Spy.OutboundEnvelopes.ShouldBeEmpty();

                // As with any batch stopping the consumer, the failure is rethrown to the message being added to the batch, which logs it
                // as fatal, but no other exception reaches the pipeline (e.g., disposing its context while it's still being processed)
                logEventsCollector.ContainsProcessingFatalError(exception => exception is not InvalidOperationException { Message: "Test" })
                    .ShouldBeFalse();
                break;
        }
    }

    [Fact]
    public async Task Chunking_ShouldCommitAndSkipRemainingChunks_WhenEnumerationStopsWhileBinaryMessageConsumedInBatch()
    {
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();
        int batchesCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<BinaryMessage>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3))))
            .AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            bool isFirstBatch = Interlocked.Increment(ref batchesCount) == 1;
            List<string?> list = [];
            batches.Add(list);

            await foreach (BinaryMessage message in batch)
            {
                // Stop enumerating the first batch while the second binary message is partly read: its first chunk is still being pushed
                // and its next chunks are pending
                if (isFirstBatch && list.Count == 1)
                {
                    byte[] buffer = new byte[5];
                    await message.Content!.ReadExactlyAsync(buffer);
                    break;
                }

                byte[]? readAll = await message.Content.ReadAllAsync();
                list.Add(readAll != null ? Encoding.UTF8.GetString(readAll) : null);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks1 = [.. "Long message 1"u8.ToArray().Chunk(5)];
        byte[][] chunks2 = [.. "Long message 2"u8.ToArray().Chunk(5)];

        for (int i = 0; i < chunks1.Length; i++)
        {
            await producer.RawProduceAsync(chunks1[i], HeadersHelper.GetChunkHeaders("1", i, chunks1.Length));
        }

        for (int i = 0; i < chunks2.Length; i++)
        {
            await producer.RawProduceAsync(chunks2[i], HeadersHelper.GetChunkHeaders("2", i, chunks2.Length));
        }

        for (int i = 3; i <= 5; i++)
        {
            await producer.RawProduceAsync(Encoding.UTF8.GetBytes($"Message {i}"), HeadersHelper.GetChunkHeaders($"{i}", 0, 1));
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);

        // The first batch is committed when its enumeration stops, and the remaining chunks of the binary message are skipped
        await AsyncTestingUtil.WaitAsync(() => DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName) >= 9);

        batches.Count.ShouldBe(2);
        batches[0].ShouldBe(["Long message 1"]);
        batches[1].ShouldBe(["Message 3", "Message 4", "Message 5"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(9);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.History.ShouldNotContain(change => change.Status == ConsumerStatus.Stopped);
        logEventsCollector.ContainsProcessingFatalError().ShouldBeFalse();
    }
}
