// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingTests
{
    [Fact]
    public async Task Chunking_ShouldDisconnectAndNotCommit_WhenBinaryMessageProcessingFailsAfterFirstChunk()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(BinaryMessage binaryMessage)
        {
            // Read first chunk only
            byte[] buffer = new byte[10];
            int dummy = binaryMessage.Content!.Read(buffer, 0, 10);

            throw new InvalidOperationException("Test");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(
            new BinaryMessage
            {
                Content = BytesUtil.GetRandomStream(),
                ContentType = "application/pdf"
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Chunking_ShouldDisconnectAndNotCommit_WhenBinaryMessageProcessingFailsImmediately()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(BinaryMessage message) => throw new InvalidOperationException("Test");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(
            new BinaryMessage
            {
                Content = BytesUtil.GetRandomStream(),
                ContentType = "application/pdf"
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Theory]
    [InlineData("retry", false)]
    [InlineData("retry", true)]
    [InlineData("skip", false)]
    [InlineData("skip", true)]
    public async Task Chunking_ShouldStopConsumerWithoutApplyingErrorPolicy_WhenCommitFailsAfterConsumerIsCommitted(
        string policy,
        bool isConsumedInBatch)
    {
        TestingCollection<string> receivedMessages = [];
        LogEventsCollector logEventsCollector = new();
        IStopConsumerErrorPolicyCallback callback = (IStopConsumerErrorPolicyCallback)Substitute.For(
            [typeof(IStopConsumerErrorPolicyCallback), typeof(IRetryErrorPolicyCallback), typeof(ISkipMessageErrorPolicyCallback)],
            []);

        // The first commit fails after the consumer was committed, as when a Committed handler throws
        TransactionEventsTracker transactionEventsTracker = new(failingCommitsCount: 1, isFailingAfterConsumerCommit: true);

        await Host.ConfigureServicesAndRunAsync(services =>
        {
            SilverbackBuilder silverbackBuilder = services
                .AddLogging(logging => logging.AddProvider(logEventsCollector))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .CommitOffsetEach(1)
                        .Consume<BinaryMessage>(endpoint =>
                        {
                            endpoint
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(errorPolicy =>
                                {
                                    if (policy == "retry")
                                        errorPolicy.Retry(1);
                                    else
                                        errorPolicy.Skip();
                                });

                            if (isConsumedInBatch)
                                endpoint.EnableBatchProcessing(2);
                        })))
                .AddSingletonBrokerBehavior(transactionEventsTracker)
                .AddSingletonErrorPolicyCallback(callback);

            if (isConsumedInBatch)
                silverbackBuilder.AddDelegateSubscriber<IAsyncEnumerable<BinaryMessage>>(HandleBatchAsync);
            else
                silverbackBuilder.AddDelegateSubscriber<BinaryMessage>(HandleMessageAsync);
        });

        async Task HandleBatchAsync(IAsyncEnumerable<BinaryMessage> batch)
        {
            await foreach (BinaryMessage message in batch)
            {
                await HandleMessageAsync(message);
            }
        }

        async Task HandleMessageAsync(BinaryMessage message)
        {
            byte[]? content = await message.Content.ReadAllAsync();

            receivedMessages.Add(Encoding.UTF8.GetString(content!));
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int messageIndex = 1; messageIndex <= 4; messageIndex++)
        {
            byte[][] chunks = [.. Encoding.UTF8.GetBytes($"Long message {messageIndex}").Chunk(5)];

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                await producer.RawProduceAsync(
                    chunks[chunkIndex],
                    HeadersHelper.GetChunkHeaders($"{messageIndex}", chunkIndex, chunks.Length));
            }
        }

        // The transaction can't be rolled back anymore: the policy isn't applied, since retrying or skipping the chunked message (or the
        // batch) requires rolling it back, and the consumer is stopped and disconnected, as with the stop policy
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(
            () => consumer.Client.Status == ClientStatus.Disconnected && consumer.StatusInfo.Status == ConsumerStatus.Stopped,
            TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.ShouldBe(isConsumedInBatch ? ["Long message 1", "Long message 2"] : ["Long message 1"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(isConsumedInBatch ? 6 : 3);

        // The last chunk of the batch might not await it (the batch can be aborted before the chunk is marked as the end of the sequence),
        // so the first chunk of the next chunked message might be consumed before the consumer is stopped, which aborts its transaction,
        // rolling it back. Nothing else is committed.
        IReadOnlyList<string> transactionEvents = transactionEventsTracker.Events;
        transactionEvents.Take(2).ShouldBe(["Committing", "Committed"]);

        if (isConsumedInBatch)
            transactionEvents.Skip(2).ShouldAllBe(transactionEvent => transactionEvent == "Aborting" || transactionEvent == "Aborted");
        else
            transactionEvents.Count.ShouldBe(2);

        // Only the stop callbacks are invoked
        await callback.Received(1).OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>());
        callback.ReceivedCalls().Count().ShouldBe(1);

        // The commit exception is logged once and rethrown to the last chunk of the chunked message, which logs it as fatal, instead of the
        // failure of the rollback. The batch is instead committed after its last chunked message was added, so no chunk rethrows it.
        logEventsCollector.Count(IntegrationLogEvents.SequenceProcessingError.EventId).ShouldBe(1);
        logEventsCollector.Count(IntegrationLogEvents.RollbackToRetryFailed.EventId).ShouldBe(0);
        logEventsCollector.Count(IntegrationLogEvents.RollbackToSkipFailed.EventId).ShouldBe(0);
        logEventsCollector.Count(IntegrationLogEvents.ProcessingConsumedMessageFatalError.EventId).ShouldBe(isConsumedInBatch ? 0 : 1);
        logEventsCollector.ContainsProcessingFatalError(exception =>
                exception?.GetBaseException() is not InvalidOperationException { Message: "Commit failed." })
            .ShouldBeFalse();
    }
}
