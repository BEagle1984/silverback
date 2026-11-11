// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Silverback.Tests.Extended.Shared.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class StaticAssignmentTests : BrokerHarnessTests<KafkaTestHarness>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public StaticAssignmentTests(KafkaFixture fixture, ITestOutputHelper output)
        : base(new KafkaTestHarness("static", output))
    {
    }

    [Theory]
    [InlineData("automatic", false, false)]
    [InlineData("per-message", false, false)]
    [InlineData("disabled", false, false)]
    [InlineData("client-side", false, false)]
    [InlineData("automatic", true, false)]
    [InlineData("per-message", true, false)]
    [InlineData("disabled", true, false)]
    [InlineData("client-side", true, false)]
    [InlineData("automatic", true, true)]
    [InlineData("per-message", true, true)]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "The teardown must stop the peer loop started later")]
    public async Task Commit_ShouldRespectGroupMembership_WithStaticAssignment(string commitMode, bool shareActiveGroup, bool retryOnError)
    {
        bool commitsToBroker = commitMode is not ("disabled" or "client-side");

        string groupId = Harness.Prefix + "-group";
        TopicPartition partition = new(Harness.Prefix + "-records", 0);
        string peerTopic = Harness.Prefix + "-peer";
        TaskCompletionSource<ReconciliationMessage> processed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ReconciliationMessage> processedAfterFailure = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<ReconciliationMessage> received = new();
        CommitObserver commits = new(partition);

        await Harness.CreateTopicsAsync(partition.Topic, peerTopic);

        if (shareActiveGroup)
        {
            // Make the peer topic available before its subscribed member requests an assignment
            await Harness.Producer.ProduceAsync(new TopicPartition(peerTopic, 0), new Message<Null, byte[]> { Value = [] });
        }

        ConcurrentQueue<string> peerDiagnostics = new();
        bool peerAssignmentEmpty = false;

        using IConsumer<Ignore, byte[]> observer = new ConsumerBuilder<Ignore, byte[]>(
            new ConsumerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false
            }).Build();

        // A subscribed member keeps the group active without consuming the statically assigned topic
        IConsumer<Ignore, byte[]> peer = new ConsumerBuilder<Ignore, byte[]>(
            new ConsumerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                TopicMetadataRefreshIntervalMs = 250,
                Debug = "cgrp,topic"
            })
            .SetLogHandler((_, message) => peerDiagnostics.Enqueue(message.Message))
            .SetPartitionsAssignedHandler((_, partitions) => peerAssignmentEmpty = partitions.Count == 0)
            .Build();
        CancellationTokenSource stopPeer = new();
        TaskCompletionSource<bool> peerReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task peerLoop = Task.CompletedTask;

        // Stop the peer after the host, as the static consumer must stop first, and dispose it with the harness, since the test method
        // ends before the harness is disposed
        Harness.OnStopped(async () =>
        {
            try
            {
                stopPeer.Cancel();
                await peerLoop.WaitAsync(Timeout);
            }
            finally
            {
                if (shareActiveGroup && !peerReady.Task.IsCompletedSuccessfully)
                {
                    foreach (string diagnostic in peerDiagnostics)
                    {
                        Harness.Output.WriteLine($"Peer: {diagnostic}");
                    }
                }

                foreach (CommittedOffsets result in commits.Results)
                {
                    Harness.Output.WriteLine($"Commit: global={result.Error}; partitions={string.Join(", ", result.Offsets ?? [])}");
                }

                peer.Dispose();
                stopPeer.Dispose();
            }
        });

        Harness.ConfigureBroker(options => options.AddInMemoryKafkaOffsetStore());
        Harness.ConfigureSilverback(silverback => silverback
            .AddSingletonBrokerClientCallback(commits)
            .AddDelegateSubscriber<ReconciliationMessage>(message =>
            {
                received.Enqueue(message);

                if (message.Sequence == 1)
                    processed.TrySetResult(message);
                else
                    processedAfterFailure.TrySetResult(message);
            }));
        Harness.AddClients(clients => clients
            .AddConsumer(consumer =>
            {
                consumer
                    .WithGroupId(groupId)
                    .WithClientId(Harness.Prefix + "-static")
                    .WithAutoCommitIntervalMs(250)
                    .DisableAutoRecovery()
                    .Consume<ReconciliationMessage>(endpoint =>
                    {
                        endpoint
                            .ConsumeFrom(new TopicPartitionOffset(partition, Offset.Beginning))
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader());

                        if (retryOnError)
                            endpoint.OnError(policy => policy.Retry(3));
                    });

                switch (commitMode)
                {
                    case "automatic":
                        consumer.EnableAutoCommit();
                        break;
                    case "per-message":
                        consumer.DisableAutoCommit().CommitOffsetEach(1);
                        break;
                    case "disabled":
                        consumer.DisableOffsetsCommit();
                        break;
                    case "client-side":
                        consumer.DisableOffsetsCommit().StoreOffsetsClientSide(store => store.UseMemory());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(commitMode));
                }
            }));

        Harness.Output.WriteLine($"librdkafka={Library.VersionString}; group={groupId}; mode={commitMode}; activeSubscribedMember={shareActiveGroup}");

        if (shareActiveGroup)
        {
            using IAdminClient peerAdmin = new DependentAdminClientBuilder(peer.Handle).Build();
            Stopwatch readiness = Stopwatch.StartNew();

            while (true)
            {
                TopicMetadata metadata = peerAdmin.GetMetadata(peerTopic, TimeSpan.FromSeconds(5)).Topics.Single();

                if (!metadata.Error.IsError && metadata.Partitions.Count == 1 && metadata.Partitions[0].Leader >= 0)
                    break;

                readiness.Elapsed.ShouldBeLessThan(Timeout);
                await Task.Delay(100);
            }

            peer.Subscribe(peerTopic);
            peerLoop = Task.Run(() =>
            {
                try
                {
                    while (!stopPeer.IsCancellationRequested)
                    {
                        try
                        {
                            peer.Consume(TimeSpan.FromMilliseconds(100));
                        }
                        catch (ConsumeException exception) when (
                            exception.Error.Code == ErrorCode.UnknownTopicOrPart && !peerReady.Task.IsCompleted)
                        {
                            // CreateTopics can return before the topic metadata has propagated to every broker
                            Harness.Output.WriteLine($"Waiting for peer topic metadata: {exception.Error}");
                        }

                        // The group leader can compute the assignment from the metadata of a broker that doesn't
                        // know the new topic yet, so rejoin to have it assigned again
                        if (peerAssignmentEmpty && !peerReady.Task.IsCompleted)
                        {
                            Harness.Output.WriteLine("Peer received an empty assignment, rejoining the group");
                            peerAssignmentEmpty = false;
                            peer.Unsubscribe();
                            peer.Subscribe(peerTopic);
                        }

                        if (peer.Assignment.Count > 0)
                            peerReady.TrySetResult(true);
                    }
                }
                finally
                {
                    peer.Close();
                }
            });

            await await Task.WhenAny(peerReady.Task, peerLoop).WaitAsync(Timeout);
            peerReady.Task.IsCompletedSuccessfully.ShouldBeTrue();
            await AssertGroupIsStableAsync();
        }

        await Harness.StartHostAsync();

        DeliveryResult<Null, byte[]> delivery = await Harness.Producer.ProduceAsync(
            partition,
            new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(new ReconciliationMessage(0, 1)) });

        (await processed.Task.WaitAsync(Timeout)).ShouldBe(new ReconciliationMessage(0, 1));

        if (!commitsToBroker)
        {
            // Observe several automatic-commit intervals before checking that processing did not persist an offset
            await Task.Delay(1000);
            commits.Results.ShouldBeEmpty();
            ReadCommittedOffset().ShouldBe(Offset.Unset);
        }
        else
        {
            CommittedOffsets result = await commits.FirstResult.Task.WaitAsync(Timeout);
            ErrorCode[] errors = GetErrors(result);

            if (shareActiveGroup)
            {
                errors.ShouldContain(ErrorCode.UnknownMemberId);
                ReadCommittedOffset().ShouldBe(Offset.Unset);
                await AssertGroupIsStableAsync();

                Harness.Logs.OfEvents(KafkaLogEvents.OffsetCommitError, IntegrationLogEvents.ConsumerCommitError).ShouldNotBeEmpty();

                await Harness.Producer.ProduceAsync(
                    partition,
                    new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(new ReconciliationMessage(0, 2)) });

                (await processedAfterFailure.Task.WaitAsync(Timeout)).ShouldBe(new ReconciliationMessage(0, 2));
                Harness.Output.WriteLine($"{commitMode}: the subscriber continued after the failed commit; retry policy enabled={retryOnError}");
            }
            else
            {
                errors.ShouldBeEmpty();
                ReadCommittedOffset().ShouldBe(delivery.Offset + 1);
            }
        }

        await Harness.StopHostAsync().WaitAsync(Timeout);

        Offset expected = !commitsToBroker || shareActiveGroup ? Offset.Unset : delivery.Offset + 1;
        ReadCommittedOffset().ShouldBe(expected);
        Harness.Logs.OfEvents(IntegrationLogEvents.ProcessingConsumedMessageError).ShouldBeEmpty();

        ReconciliationMessage[] expectedMessages = shareActiveGroup && commitsToBroker
            ? [new ReconciliationMessage(0, 1), new ReconciliationMessage(0, 2)]
            : [new ReconciliationMessage(0, 1)];
        received.ShouldBe(expectedMessages);

        async Task AssertGroupIsStableAsync()
        {
            DescribeConsumerGroupsResult result = await Harness.Admin.DescribeConsumerGroupsAsync([groupId]).WaitAsync(Timeout);
            ConsumerGroupDescription group = result.ConsumerGroupDescriptions.Single();
            Harness.Output.WriteLine($"Group state: {group.State}; members: {group.Members.Count}");

            group.State.ShouldBe(ConsumerGroupState.Stable);
            group.Members.Count.ShouldBe(1);
            peerLoop.IsCompleted.ShouldBeFalse();
        }

        Offset ReadCommittedOffset()
        {
            Offset offset = observer.Committed([partition], TimeSpan.FromSeconds(5)).Single().Offset;
            Harness.Output.WriteLine($"Broker committed offset: {partition} = {offset}");

            return offset;
        }
    }

    private static ErrorCode[] GetErrors(CommittedOffsets result) =>
    [
        .. new[] { result.Error.Code }
            .Concat(result.Offsets?.Select(offset => offset.Error.Code) ?? [])
            .Where(code => code != ErrorCode.NoError)
    ];

    private sealed class CommitObserver : IKafkaOffsetCommittedCallback
    {
        private readonly TopicPartition _partition;

        public CommitObserver(TopicPartition partition)
        {
            _partition = partition;
        }

        public ConcurrentQueue<CommittedOffsets> Results { get; } = new();

        public TaskCompletionSource<CommittedOffsets> FirstResult { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnOffsetsCommitted(CommittedOffsets offsets, IKafkaConsumer consumer)
        {
            Results.Enqueue(offsets);

            if (offsets.Error.IsError || offsets.Offsets?.Any(offset => offset.TopicPartition == _partition) == true)
                FirstResult.TrySetResult(offsets);
        }
    }
}
