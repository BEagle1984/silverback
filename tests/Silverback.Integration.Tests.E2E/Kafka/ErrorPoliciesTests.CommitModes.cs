// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesTests
{
    private const int ChunksCount = 3;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "TestData")]
    public static TheoryData<string, string, bool> ErrorPolicy_ShouldHandleFailure_TestData
    {
        get
        {
            TheoryData<string, string, bool> data = [];

            foreach (string policy in new[] { "retry", "skip", "stop" })
            {
                foreach (string commitMode in new[] { "auto", "manual", "disabled", "store" })
                {
                    data.Add(policy, commitMode, false);
                    data.Add(policy, commitMode, true);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ErrorPolicy_ShouldHandleFailure_TestData))]
    public async Task ErrorPolicy_ShouldHandleFailure_WhenSubscriberFailsOnce(string policy, string commitMode, bool batch)
    {
        int batchSize = batch ? 2 : 1;
        string[][] chunks =
        [
            .. Enumerable.Range(1, ChunksCount * batchSize)
                .Select(index => index.ToString(CultureInfo.InvariantCulture))
                .Chunk(batchSize)
        ];
        List<string> received = [];
        int failuresCount = 0;

        await Host.ConfigureServicesAndRunAsync(services =>
        {
            SilverbackBuilder silverback = services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                    .AddInMemoryKafkaOffsetStore())
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer(consumer => ConfigureConsumer(consumer, policy, commitMode, batchSize)));

            if (batch)
                silverback.AddDelegateSubscriber<IEnumerable<TestEventOne>>(messages => HandleMessages([.. messages]));
            else
                silverback.AddDelegateSubscriber<TestEventOne>(message => HandleMessages([message]));
        });

        // The second chunk fails, with the offsets of the first one already committed or stored
        void HandleMessages(IReadOnlyCollection<TestEventOne> messages)
        {
            lock (received)
            {
                received.AddRange(messages.Select(message => message.ContentEventOne!));
            }

            if (messages.First().ContentEventOne == chunks[1][0] && Interlocked.Increment(ref failuresCount) == 1)
                throw new InvalidOperationException("Failed once!");
        }

        // A failed rollback would make the error policy reconnect the consumer, which would consume the message again as well
        int disconnectionsCount = 0;
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.Client.Disconnecting.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnectionsCount);
            return ValueTask.CompletedTask;
        });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        foreach (string content in chunks.SelectMany(chunk => chunk))
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = content });
        }

        bool isStopping = policy == "stop";
        bool isCommitting = commitMode is "auto" or "manual";

        await WaitUntilCompletedAsync(consumer, isStopping, isCommitting);

        string[] expectedMessages = policy switch
        {
            "stop" => [.. chunks[0], .. chunks[1]],
            "skip" => [.. chunks[0], .. chunks[1], .. chunks[2]],
            _ => [.. chunks[0], .. chunks[1], .. chunks[1], .. chunks[2]]
        };

        lock (received)
        {
            received.ShouldBe(expectedMessages);
        }

        AssertConsumerStatus(consumer, isStopping, disconnectionsCount);

        // Only the chunk preceding the failure is completed, when the consumer stops
        int completedMessagesCount = isStopping ? batchSize : ChunksCount * batchSize;
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(isCommitting ? completedMessagesCount : 0);

        if (commitMode == "store")
            AssertStoredOffset(consumer, completedMessagesCount - 1);
    }

    private static void AssertConsumerStatus(IConsumer consumer, bool isStopping, int disconnectionsCount)
    {
        if (isStopping)
        {
            consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        }
        else
        {
            consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
            consumer.StatusInfo.Status.ShouldNotBe(ConsumerStatus.Stopped);
            disconnectionsCount.ShouldBe(0);
        }
    }

    private static void ConfigureCommitMode(KafkaConsumerConfigurationBuilder consumer, string commitMode)
    {
        switch (commitMode)
        {
            case "manual":
                consumer.DisableAutoCommit().CommitOffsetEach(1);
                break;
            case "disabled":
                consumer.DisableOffsetsCommit();
                break;
            case "store":
                consumer.DisableOffsetsCommit().StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory());
                break;
        }
    }

    private static void ConfigureConsumer(KafkaConsumerConfigurationBuilder consumer, string policy, string commitMode, int batchSize)
    {
        ConfigureCommitMode(consumer, commitMode);

        consumer
            .WithGroupId(DefaultGroupId)
            .Consume<TestEventOne>(endpoint =>
            {
                endpoint
                    .ConsumeFrom(DefaultTopicName)
                    .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy));

                if (batchSize > 1)
                    endpoint.EnableBatchProcessing(batchSize);
            });
    }

    private async Task WaitUntilCompletedAsync(IConsumer consumer, bool isStopping, bool isCommitting)
    {
        if (isStopping)
            await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));
        else if (isCommitting)
            await Helper.WaitUntilAllMessagesAreCommittedAsync();
        else
            await Helper.WaitUntilAllMessagesAreConsumedAsync();
    }

    private void AssertStoredOffset(IConsumer consumer, int expectedOffset)
    {
        IKafkaOffsetStore offsetStore = Host.ServiceProvider
            .GetRequiredService<IKafkaOffsetStoreFactory>()
            .GetStore(((KafkaConsumer)consumer).Configuration.ClientSideOffsetStore!, Host.ServiceProvider);

        offsetStore.GetStoredOffsets(DefaultGroupId).Single().Offset.Value.ShouldBe(expectedOffset);
    }
}
