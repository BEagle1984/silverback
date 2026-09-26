// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Silverback.Tests.Extended.Shared.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class StaticAssignmentTests(KafkaFixture fixture, ITestOutputHelper output)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

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
    public async Task Commit_ShouldRespectGroupMembership_WithStaticAssignment(string commitMode, bool shareActiveGroup, bool retryOnError)
    {
        _ = fixture;

        bool commitsToBroker = commitMode is not ("disabled" or "client-side");

        string prefix = "stress-static-" + Guid.NewGuid().ToString("N");
        string groupId = prefix + "-group";
        TopicPartition partition = new(prefix + "-records", 0);
        string peerTopic = prefix + "-peer";
        TaskCompletionSource<ReconciliationMessage> processed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ReconciliationMessage> processedAfterFailure = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<ReconciliationMessage> received = new();
        CommitObserver commits = new(partition);
        using CommitLoggerProvider logs = new();

        using IAdminClient admin = new AdminClientBuilder(
            new AdminClientConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers
            }).Build();
        await admin.CreateTopicsAsync(
        [
            new TopicSpecification { Name = partition.Topic, NumPartitions = 1, ReplicationFactor = 1 },
            new TopicSpecification { Name = peerTopic, NumPartitions = 1, ReplicationFactor = 1 }
        ]);

        using IProducer<Null, byte[]> producer = new ProducerBuilder<Null, byte[]>(
            new ProducerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                Acks = Acks.All
            }).Build();

        if (shareActiveGroup)
        {
            // Make the peer topic available before its subscribed member requests an assignment
            await producer.ProduceAsync(new TopicPartition(peerTopic, 0), new Message<Null, byte[]> { Value = [] });
        }

        ConcurrentQueue<string> peerDiagnostics = new();

        using IConsumer<Ignore, byte[]> observer = new ConsumerBuilder<Ignore, byte[]>(
            new ConsumerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false
            }).Build();

        // A subscribed member keeps the group active without consuming the statically assigned topic
        using IConsumer<Ignore, byte[]> peer = new ConsumerBuilder<Ignore, byte[]>(
            new ConsumerConfig
            {
                BootstrapServers = KafkaFixture.BootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                TopicMetadataRefreshIntervalMs = 250,
                Debug = "cgrp,topic"
            }).SetLogHandler((_, message) => peerDiagnostics.Enqueue(message.Message)).Build();
        using CancellationTokenSource stopPeer = new();
        TaskCompletionSource<bool> peerReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task peerLoop = Task.CompletedTask;

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka().AddInMemoryKafkaOffsetStore())
            .AddSingletonBrokerClientCallback(commits)
            .AddDelegateSubscriber<ReconciliationMessage>(message =>
            {
                received.Enqueue(message);

                if (message.Sequence == 1)
                    processed.TrySetResult(message);
                else
                    processedAfterFailure.TrySetResult(message);
            })
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(KafkaFixture.BootstrapServers)
                .AddConsumer(consumer =>
                {
                    consumer
                        .WithGroupId(groupId)
                        .WithClientId(prefix + "-static")
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

        using IHost host = builder.Build();

        output.WriteLine($"librdkafka={Library.VersionString}; group={groupId}; mode={commitMode}; activeSubscribedMember={shareActiveGroup}");

        try
        {
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
                                output.WriteLine($"Waiting for peer topic metadata: {exception.Error}");
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

            await host.StartAsync().WaitAsync(Timeout);

            DeliveryResult<Null, byte[]> delivery = await producer.ProduceAsync(
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

                    logs.CommitErrors.ShouldNotBeEmpty();

                    await producer.ProduceAsync(
                        partition,
                        new Message<Null, byte[]> { Value = JsonSerializer.SerializeToUtf8Bytes(new ReconciliationMessage(0, 2)) });

                    (await processedAfterFailure.Task.WaitAsync(Timeout)).ShouldBe(new ReconciliationMessage(0, 2));
                    output.WriteLine($"{commitMode}: the subscriber continued after the failed commit; retry policy enabled={retryOnError}");
                }
                else
                {
                    errors.ShouldBeEmpty();
                    ReadCommittedOffset().ShouldBe(delivery.Offset + 1);
                }
            }

            await host.StopAsync().WaitAsync(Timeout);

            Offset expected = !commitsToBroker || shareActiveGroup ? Offset.Unset : delivery.Offset + 1;
            ReadCommittedOffset().ShouldBe(expected);
            logs.ProcessingErrors.ShouldBeEmpty();

            ReconciliationMessage[] expectedMessages = shareActiveGroup && commitsToBroker
                ? [new ReconciliationMessage(0, 1), new ReconciliationMessage(0, 2)]
                : [new ReconciliationMessage(0, 1)];
            received.ShouldBe(expectedMessages);
        }
        finally
        {
            try
            {
                await host.StopAsync().WaitAsync(Timeout);
            }
            finally
            {
                stopPeer.Cancel();
                await peerLoop.WaitAsync(Timeout);

                if (shareActiveGroup && !peerReady.Task.IsCompletedSuccessfully)
                {
                    foreach (string diagnostic in peerDiagnostics)
                    {
                        output.WriteLine($"Peer: {diagnostic}");
                    }
                }

                foreach (string error in logs.Errors)
                {
                    output.WriteLine($"Error log: {error}");
                }

                foreach (CommittedOffsets result in commits.Results)
                {
                    output.WriteLine($"Commit: global={result.Error}; partitions={string.Join(", ", result.Offsets ?? [])}");
                }
            }
        }

        async Task AssertGroupIsStableAsync()
        {
            DescribeConsumerGroupsResult result = await admin.DescribeConsumerGroupsAsync([groupId]).WaitAsync(Timeout);
            ConsumerGroupDescription group = result.ConsumerGroupDescriptions.Single();
            output.WriteLine($"Group state: {group.State}; members: {group.Members.Count}");

            group.State.ShouldBe(ConsumerGroupState.Stable);
            group.Members.Count.ShouldBe(1);
            peerLoop.IsCompleted.ShouldBeFalse();
        }

        Offset ReadCommittedOffset()
        {
            Offset offset = observer.Committed([partition], TimeSpan.FromSeconds(5)).Single().Offset;
            output.WriteLine($"Broker committed offset: {partition} = {offset}");

            return offset;
        }
    }

    private static ErrorCode[] GetErrors(CommittedOffsets result) =>
    [
        .. new[] { result.Error.Code }
            .Concat(result.Offsets?.Select(offset => offset.Error.Code) ?? [])
            .Where(code => code != ErrorCode.NoError)
    ];

    private sealed class CommitLoggerProvider : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<string> CommitErrors { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public ConcurrentQueue<string> ProcessingErrors { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId == KafkaLogEvents.OffsetCommitError.EventId || eventId == IntegrationLogEvents.ConsumerCommitError.EventId)
                CommitErrors.Enqueue(formatter(state, exception));

            if (eventId == IntegrationLogEvents.ProcessingConsumedMessageError.EventId)
                ProcessingErrors.Enqueue(formatter(state, exception));

            if (logLevel >= LogLevel.Error)
                Errors.Enqueue($"{eventId.Id}: {formatter(state, exception)}");
        }

        public void Dispose()
        {
            // No resources to release
        }
    }

    private sealed class CommitObserver(TopicPartition partition) : IKafkaOffsetCommittedCallback
    {
        public ConcurrentQueue<CommittedOffsets> Results { get; } = new();

        public TaskCompletionSource<CommittedOffsets> FirstResult { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnOffsetsCommitted(CommittedOffsets offsets, IKafkaConsumer consumer)
        {
            Results.Enqueue(offsets);

            if (offsets.Error.IsError || offsets.Offsets?.Any(offset => offset.TopicPartition == partition) == true)
                FirstResult.TrySetResult(offsets);
        }
    }
}
