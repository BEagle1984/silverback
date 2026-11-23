// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task Start_ShouldStopLoopAndDisconnect_WhenRestoringPartitionsFails()
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);

        TaskCompletionSource<bool> disconnected = NewSignal();
        harness.Client.DisconnectAsync().Returns(_ =>
        {
            disconnected.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        harness.Client.When(client => client.Seek(Arg.Any<TopicPartitionOffset>()))
            .Do(_ => throw new InvalidOperationException("Seek failed"));

        // The restarted loop seeks back to the uncommitted record before its first poll
        await harness.Consumer.StopAsync(false).AsTask().WaitAsync(Timeout);
        await harness.Consumer.StartAsync().AsTask().WaitAsync(Timeout);

        await disconnected.Task.WaitAsync(Timeout);
        await harness.PollingStopped.WaitAsync(Timeout);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        harness.Errors.ShouldHaveSingleItem().ShouldStartWith(
            "Error occurred trying to pull next message; the consumer will be stopped (auto recovery disabled for consumer)");
        await harness.Client.Received(1).DisconnectAsync();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The seek is awaited before the harness is disposed.")]
    public async Task Start_ShouldStopLoopWithoutError_WhenRestoringPartitionsFailsWhileStopping()
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);

        // The consume loop is stopped while it is seeking, so the resulting exception is expected
        TaskCompletionSource<Task> stopRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Client.When(client => client.Seek(Arg.Any<TopicPartitionOffset>())).Do(_ =>
        {
            stopRequested.TrySetResult(harness.StopPollingAsync());
            throw new InvalidOperationException("Seek failed");
        });

        await harness.Consumer.StopAsync(false).AsTask().WaitAsync(Timeout);
        await harness.Consumer.StartAsync().AsTask().WaitAsync(Timeout);

        Task stopping = await stopRequested.Task.WaitAsync(Timeout);
        await stopping.WaitAsync(Timeout);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        harness.Logs.ShouldContain(log => log.StartsWith("Consuming canceled", StringComparison.Ordinal));
        harness.Errors.ShouldBeEmpty();
        await harness.Client.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The poll is released before the event is disposed.")]
    public async Task Start_ShouldNotRestorePartitions_WhenLoopIsStoppedBeforePreviousLoopExits()
    {
        await using PollHarness harness = new();
        await harness.StartAsync(0);

        ProcessingGate active = harness.Block(0, 0);
        active.SkipCommit = true;
        await harness.DeliverAsync((0, 0));
        await active.Started.Task.WaitAsync(Timeout);

        ConsumeLoopHandler consumeLoopHandler = GetConsumeLoopHandlerOfRestoreTests(harness);
        TaskCompletionSource<bool> pollEntered = NewSignal();
        using ManualResetEventSlim releasePoll = new();
        Task heldPoll = harness.PollAsync(() =>
        {
            pollEntered.TrySetResult(true);
            releasePoll.Wait(Timeout);

            return null;
        });
        await pollEntered.Task.WaitAsync(Timeout);

        // The new loop waits for the previous one, still inside Consume, and is stopped before it could seek
        Task firstStop = consumeLoopHandler.StopAsync();
        consumeLoopHandler.Start();
        Task secondStop = consumeLoopHandler.StopAsync();

        releasePoll.Set();
        await Task.WhenAll(heldPoll, firstStop, secondStop).WaitAsync(Timeout);

        harness.Seeks.ShouldBeEmpty();
        harness.Logs.Count(log => log.StartsWith("Consume loop stopped", StringComparison.Ordinal)).ShouldBe(2);

        // A loop that isn't stopped in the meantime seeks back to the uncommitted record before its first poll
        consumeLoopHandler.Start();
        await harness.PollAsync(() => null);

        harness.Seeks.ShouldBe([new TopicPartitionOffset("topic", 0, 0)]);
        harness.Errors.ShouldBeEmpty();
        await harness.Client.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    public async Task OnPollTimeout_ShouldLogAndReconnect_WhenAutoRecoveryIsEnabled()
    {
        await using PipelineHarness harness = new(CatchUpMode.None, autoRecovery: true);
        await harness.StartAsync(0);

        harness.Consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Connected);

        bool handled = harness.Consumer.OnPollTimeout(CreatePollTimeoutLogMessage());

        handled.ShouldBeTrue();
        await harness.Reconnected.Task.WaitAsync(Timeout);
        harness.Logs.ShouldContain(log => log.EventId == KafkaLogEvents.PollTimeoutAutoRecovery.EventId.Id && log.Level == LogLevel.Warning);
        harness.Logs.ShouldNotContain(log => log.EventId == KafkaLogEvents.PollTimeoutNoAutoRecovery.EventId.Id);
    }

    [Fact]
    public async Task OnPollTimeout_ShouldLogAndRevertConnectedStatus_WhenAutoRecoveryIsDisabled()
    {
        await using PipelineHarness harness = new(CatchUpMode.None);
        await harness.StartAsync(0);

        harness.Consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Connected);

        bool handled = harness.Consumer.OnPollTimeout(CreatePollTimeoutLogMessage());

        handled.ShouldBeTrue();
        harness.Consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Started);
        harness.Logs.ShouldContain(log => log.EventId == KafkaLogEvents.PollTimeoutNoAutoRecovery.EventId.Id && log.Level == LogLevel.Error);
        harness.Reconnected.Task.IsCompleted.ShouldBeFalse();
    }

    private static LogMessage CreatePollTimeoutLogMessage() =>
        new("rdkafka#consumer-1", SyslogLevel.Warning, "MAXPOLL", "Application maximum poll interval (300000ms) exceeded by 42ms");

    private static ConsumeLoopHandler GetConsumeLoopHandlerOfRestoreTests(PollHarness harness)
    {
        ConsumeLoopHandler? consumeLoopHandler = typeof(KafkaConsumer)
            .GetField("_consumeLoopHandler", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(harness.Consumer) as ConsumeLoopHandler;

        consumeLoopHandler.ShouldNotBeNull();

        return consumeLoopHandler;
    }
}
