// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldCompletePendingBatchAfterTimeout()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(200)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => completedBatches == 2);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldAddMessageToNextBatch_WhenRefusedByTimedOutBatchThatIsSkipped()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> firstMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(2))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
                firstMessageReceived.TrySetResult(true);
            }

            // Skip the first batch, unless the timing didn't allow it to refuse the third message
            if (list is ["1", "2"])
                throw new InvalidOperationException("Skip!");

            successfulBatches.Add(list);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await BatchTimeoutTiming.ProduceWhileFirstBatchTimesOutAsync(
            () => publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" }),
            () => publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" }),
            () => publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" }),
            firstMessageReceived.Task);

        // The refused message isn't acknowledged with the skipped batch, but added to the next one
        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Any(batch => batch.Contains("3")));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        successfulBatches.SelectMany(batch => batch).ShouldContain("3");
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldProcessRefusedMessageOnce_WhenTimedOutBatchIsRetried()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource<bool> firstMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int failedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(10, TimeSpan.FromSeconds(2))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                    firstMessageReceived.TrySetResult(true);
                }
            }
            catch (OperationCanceledException)
            {
                // A batch aborted by the rollback (e.g. started by the third message, if the timing didn't allow the refusal)
                return;
            }

            // Fail the first batch once, unless the timing didn't allow it to refuse the third message
            if (list is ["1", "2"] && Interlocked.Increment(ref failedBatches) == 1)
                throw new InvalidOperationException("Retry!");

            successfulBatches.Add(list);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await BatchTimeoutTiming.ProduceWhileFirstBatchTimesOutAsync(
            () => publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" }),
            () => publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" }),
            () => publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" }),
            firstMessageReceived.Task);

        await AsyncTestingUtil.WaitAsync(() => successfulBatches.Any(batch => batch.Contains("3")));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The refused message is replayed by the rollback and processed once (see SequenceBaseTests for the refusal itself)
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
