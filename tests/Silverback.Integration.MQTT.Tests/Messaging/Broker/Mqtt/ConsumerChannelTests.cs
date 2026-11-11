// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class ConsumerChannelTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ReadNextAsync_ShouldReturnBufferedMessagesInOrder()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);

        readMessages.ShouldBe(messages);
        messages.ShouldAllBe(message => message.ReplayCount == 0);
    }

    [Fact]
    public async Task ReadNextAsync_ShouldReturnMessagesToReplayFirst_WhenMessagesAreRolledBack()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 4);
        await ReadMessagesAsync(channel, 3);

        channel.Rollback([messages[2], messages[1]]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);

        readMessages.ShouldBe([messages[1], messages[2], messages[3]]);
        messages.Select(message => message.ReplayCount).ShouldBe([0, 1, 1, 0]);
    }

    [Fact]
    public async Task ReadNextAsync_ShouldReturnMessagesToReplayBeforeMessageReadWhileWaiting()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message1 = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message1, out _).ShouldBe(ConsumerChannel.PendingState.Untracked);

        Task<ConsumedApplicationMessage> readTask = channel.ReadNextAsync().AsTask();
        readTask.IsCompleted.ShouldBeFalse();

        channel.Rollback([message1]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        ConsumedApplicationMessage message2 = await WriteMessageAsync(channel, 2);

        ConsumedApplicationMessage readMessage1 = await readTask.WaitAsync(Timeout);
        ConsumedApplicationMessage readMessage2 = await ReadNextAsync(channel);

        ConsumedApplicationMessage message3 = await WriteMessageAsync(channel, 3);
        ConsumedApplicationMessage readMessage3 = await ReadNextAsync(channel);

        readMessage1.ShouldBeSameAs(message1);
        readMessage2.ShouldBeSameAs(message2);
        readMessage3.ShouldBeSameAs(message3);
        message1.ReplayCount.ShouldBe(1);
        message2.ReplayCount.ShouldBe(1);
        message3.ReplayCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReadNextAsync_ShouldThrowAndKeepBufferedMessages_WhenRestartIsPending()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);
        channel.Defer(messages[0], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.Rollback([messages[0]]).ShouldBeTrue();

        channel.IsRestartPending.ShouldBeTrue();
        await Should.ThrowAsync<OperationCanceledException>(ReadNextAsync(channel));
        await Should.ThrowAsync<OperationCanceledException>(ReadNextAsync(channel));

        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);

        readMessages.ShouldBe(messages);
    }

    [Fact]
    public async Task ReadNextAsync_ShouldThrowAndKeepReadMessage_WhenRestartIsRequestedWhileWaiting()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message1 = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message1, out _).ShouldBe(ConsumerChannel.PendingState.Untracked);

        Task<ConsumedApplicationMessage> readTask = channel.ReadNextAsync().AsTask();
        readTask.IsCompleted.ShouldBeFalse();

        channel.Rollback([message1]).ShouldBeTrue();
        ConsumedApplicationMessage message2 = await WriteMessageAsync(channel, 2);

        await Should.ThrowAsync<OperationCanceledException>(readTask.WaitAsync(Timeout));
        message2.PendingNode.ShouldNotBeNull();
        message2.IsReplayPending.ShouldBeTrue();

        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        ConsumedApplicationMessage message3 = await WriteMessageAsync(channel, 3);
        ConsumedApplicationMessage readMessage3 = await ReadNextAsync(channel);

        readMessages.ShouldBe([message1, message2]);
        readMessage3.ShouldBeSameAs(message3);
    }

    [Fact]
    public async Task ReadNextAsync_ShouldResetMessage_WhenMessageIsReplayed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.MarkTracked(message);
        channel.Defer(message, out _).ShouldBe(ConsumerChannel.PendingState.Tracked);
        TaskCompletionSource<bool> rolledBackTaskCompletionSource = message.TaskCompletionSource;
        channel.Rollback([message]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);

        readMessage.ShouldBeSameAs(message);
        message.ReplayCount.ShouldBe(1);
        message.IsReplayPending.ShouldBeFalse();
        message.IsDeferred.ShouldBeFalse();
        message.IsTracked.ShouldBeFalse();
        message.TaskCompletionSource.ShouldNotBeSameAs(rolledBackTaskCompletionSource);
        message.TaskCompletionSource.Task.IsCompleted.ShouldBeFalse();
        message.TaskCompletionSource.Task.CreationOptions.ShouldBe(TaskCreationOptions.RunContinuationsAsynchronously);
        rolledBackTaskCompletionSource.Task.CreationOptions.ShouldBe(TaskCreationOptions.RunContinuationsAsynchronously);
        (await rolledBackTaskCompletionSource.Task.WaitAsync(Timeout)).ShouldBeFalse();
    }

    [Fact]
    public async Task Defer_ShouldReturnUntracked_WhenMessageIsPendingAndNotTracked()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);

        ConsumerChannel.PendingState state = channel.Defer(message, out int replayCount);

        state.ShouldBe(ConsumerChannel.PendingState.Untracked);
        replayCount.ShouldBe(0);
        message.IsDeferred.ShouldBeTrue();
    }

    [Fact]
    public async Task Defer_ShouldReturnTracked_WhenMessageIsMarkedTracked()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.MarkTracked(message);

        ConsumerChannel.PendingState state = channel.Defer(message, out int replayCount);

        state.ShouldBe(ConsumerChannel.PendingState.Tracked);
        replayCount.ShouldBe(0);
        message.IsDeferred.ShouldBeTrue();
    }

    [Fact]
    public async Task Defer_ShouldReturnResolved_WhenMessageIsAcknowledged()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.MarkTracked(message);
        channel.Acknowledged(message);

        ConsumerChannel.PendingState state = channel.Defer(message, out int replayCount);

        state.ShouldBe(ConsumerChannel.PendingState.Resolved);
        replayCount.ShouldBe(0);
        message.IsDeferred.ShouldBeFalse();
    }

    [Fact]
    public async Task Defer_ShouldReturnResolved_WhenMessageIsRolledBack()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Rollback([message]).ShouldBeFalse();

        ConsumerChannel.PendingState state = channel.Defer(message, out int replayCount);

        state.ShouldBe(ConsumerChannel.PendingState.Resolved);
        replayCount.ShouldBe(0);
        message.IsDeferred.ShouldBeFalse();
    }

    [Fact]
    public async Task Defer_ShouldReturnResolved_WhenAcknowledgmentFailed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.FailAcknowledgment(message, new InvalidOperationException("test")).ShouldBeTrue();

        ConsumerChannel.PendingState state = channel.Defer(message, out _);

        state.ShouldBe(ConsumerChannel.PendingState.Resolved);
        message.IsDeferred.ShouldBeFalse();
    }

    [Fact]
    public async Task Defer_ShouldReturnResolved_WhenReplayIsPending()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.MarkTracked(message);
        channel.PutBack(message);

        ConsumerChannel.PendingState state = channel.Defer(message, out _);

        state.ShouldBe(ConsumerChannel.PendingState.Resolved);
        message.TaskCompletionSource.Task.IsCompleted.ShouldBeFalse();
        message.IsDeferred.ShouldBeFalse();
    }

    [Fact]
    public async Task Defer_ShouldReturnReplayCount_WhenMessageIsReplayed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Rollback([message]).ShouldBeFalse();
        await ReadNextAsync(channel);
        channel.Rollback([message]).ShouldBeFalse();
        await ReadNextAsync(channel);

        ConsumerChannel.PendingState state = channel.Defer(message, out int replayCount);

        state.ShouldBe(ConsumerChannel.PendingState.Untracked);
        replayCount.ShouldBe(2);
    }

    [Fact]
    public async Task PutBack_ShouldReplayMessageBeforeBufferedMessages()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);
        Task<bool> task = messages[0].TaskCompletionSource.Task;

        channel.PutBack(messages[0]);

        task.IsCompleted.ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe(messages);
        messages[0].ReplayCount.ShouldBe(1);
        messages[1].ReplayCount.ShouldBe(0);
    }

    [Fact]
    public async Task PutBack_ShouldIgnoreMessage_WhenMessageIsAcknowledged()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);
        channel.Acknowledged(messages[0]);

        channel.PutBack(messages[0]);

        messages[0].IsReplayPending.ShouldBeFalse();
        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);
        readMessage.ShouldBeSameAs(messages[1]);
    }

    [Fact]
    public async Task PutBack_ShouldIgnoreMessage_WhenAcknowledgmentFailed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);
        channel.FailAcknowledgment(messages[0], new InvalidOperationException("test")).ShouldBeTrue();

        channel.PutBack(messages[0]);

        messages[0].IsReplayPending.ShouldBeFalse();
        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);
        readMessage.ShouldBeSameAs(messages[1]);
    }

    [Fact]
    public async Task PutBack_ShouldIgnoreMessage_WhenChannelIsReset()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage staleMessage = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Reset();

        channel.PutBack(staleMessage);

        staleMessage.IsReplayPending.ShouldBeFalse();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 2);
        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);
        readMessage.ShouldBeSameAs(message);
        staleMessage.ReplayCount.ShouldBe(0);
    }

    [Fact]
    public async Task Acknowledged_ShouldRemoveMessageAndCompleteTaskWithTrue()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);
        Task<bool> task = messages[0].TaskCompletionSource.Task;

        channel.Acknowledged(messages[0]);

        (await task.WaitAsync(Timeout)).ShouldBeTrue();
        messages[0].PendingNode.ShouldBeNull();
        channel.Rollback([messages[0]]).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);
        messages[0].IsReplayPending.ShouldBeFalse();
        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);
        readMessage.ShouldBeSameAs(messages[1]);
    }

    [Fact]
    public async Task Acknowledged_ShouldRemoveMessageToBeReplayed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);
        await ReadMessagesAsync(channel, 2);
        channel.Rollback([messages[0], messages[1]]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();

        channel.Acknowledged(messages[0]);

        messages[0].PendingNode.ShouldBeNull();
        messages[0].IsReplayPending.ShouldBeFalse();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe([messages[1], messages[2]]);
        messages[0].ReplayCount.ShouldBe(0);
    }

    [Fact]
    public async Task FailAcknowledgment_ShouldFaultTaskAndReturnTrue_WhenReaderIsProcessingMessage()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        InvalidOperationException exception = new("test");

        bool isObservedByReader = channel.FailAcknowledgment(message, exception);

        isObservedByReader.ShouldBeTrue();
        InvalidOperationException thrownException =
            await Should.ThrowAsync<InvalidOperationException>(message.TaskCompletionSource.Task.WaitAsync(Timeout));
        thrownException.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task FailAcknowledgment_ShouldReturnTrue_WhenReaderIsProcessingReplayedMessage()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);

        for (int i = 0; i < 2; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        channel.Rollback([messages[0]]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        ConsumedApplicationMessage replayedMessage = await ReadNextAsync(channel);
        replayedMessage.ShouldBeSameAs(messages[0]);

        bool isObservedByReader = channel.FailAcknowledgment(messages[0], new InvalidOperationException("test"));

        isObservedByReader.ShouldBeTrue();
        messages[0].TaskCompletionSource.Task.IsFaulted.ShouldBeTrue();
    }

    [Fact]
    public async Task FailAcknowledgment_ShouldFaultTaskAndReturnFalse_WhenMessageIsDeferred()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.MarkTracked(message);
        channel.Defer(message, out _).ShouldBe(ConsumerChannel.PendingState.Tracked);
        InvalidOperationException exception = new("test");

        bool isObservedByReader = channel.FailAcknowledgment(message, exception);

        isObservedByReader.ShouldBeFalse();
        InvalidOperationException thrownException =
            await Should.ThrowAsync<InvalidOperationException>(message.TaskCompletionSource.Task.WaitAsync(Timeout));
        thrownException.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task FailAcknowledgment_ShouldFaultTaskAndReturnFalse_WhenReaderReadNextMessage()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadMessagesAsync(channel, 2);
        InvalidOperationException exception = new("test");

        bool isObservedByReader = channel.FailAcknowledgment(messages[0], exception);

        isObservedByReader.ShouldBeFalse();
        InvalidOperationException thrownException =
            await Should.ThrowAsync<InvalidOperationException>(messages[0].TaskCompletionSource.Task.WaitAsync(Timeout));
        thrownException.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task Rollback_ShouldReplayRolledBackAndLaterPendingMessages()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 4);

        for (int i = 0; i < 3; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        Task<bool>[] tasks = [.. messages.Select(message => message.TaskCompletionSource.Task)];

        bool isRestartRequired = channel.Rollback([messages[1]]);

        isRestartRequired.ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.IsRestartPending.ShouldBeTrue();
        tasks[0].IsCompleted.ShouldBeFalse();
        (await tasks[1].WaitAsync(Timeout)).ShouldBeFalse();
        (await tasks[2].WaitAsync(Timeout)).ShouldBeFalse();
        tasks[3].IsCompleted.ShouldBeFalse();
        messages.Select(message => message.IsReplayPending).ShouldBe([false, true, true, false]);

        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);
        readMessages.ShouldBe([messages[1], messages[2], messages[3]]);
    }

    [Fact]
    public async Task Rollback_ShouldReturnFalseAndReplayMessage_WhenMessageIsProcessedByReader()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);
        Task<bool> task = messages[0].TaskCompletionSource.Task;

        bool isRestartRequired = channel.Rollback([messages[0]]);

        isRestartRequired.ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);
        channel.IsRestartPending.ShouldBeFalse();
        (await task.WaitAsync(Timeout)).ShouldBeFalse();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe(messages);
        messages[0].ReplayCount.ShouldBe(1);
    }

    [Fact]
    public async Task Rollback_ShouldReturnTrue_WhenMessageProcessedByReaderIsTracked()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadNextAsync(channel);

        // Tracked by its sequence (e.g. the batch it was added to) before the reader moved on: the restart must abort the sequence
        channel.MarkTracked(messages[0]);
        bool isRestartRequired = channel.Rollback([messages[0]]);

        isRestartRequired.ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.IsRestartPending.ShouldBeTrue();
        messages[0].IsReplayPending.ShouldBeTrue();
        messages[1].IsReplayPending.ShouldBeFalse();

        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe(messages);
        messages.Select(message => message.ReplayCount).ShouldBe([1, 0]);
    }

    [Fact]
    public async Task Rollback_ShouldReturnTrue_WhenMessageProcessedByReaderIsDeferred()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out _).ShouldBe(ConsumerChannel.PendingState.Untracked);

        bool isRestartRequired = channel.Rollback([message]);

        isRestartRequired.ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        message.IsReplayPending.ShouldBeTrue();
    }

    [Fact]
    public async Task Rollback_ShouldReturnTrue_WhenSeveralMessagesAreRolledBack()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadMessagesAsync(channel, 2);

        bool isRestartRequired = channel.Rollback([messages[1], messages[0]]);

        isRestartRequired.ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe(messages);
    }

    [Fact]
    public async Task Rollback_ShouldReturnTrue_WhenReplayedMessageIsProcessedByReaderAfterLaterMessageWasRead()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);
        await ReadMessagesAsync(channel, 2);
        channel.Rollback([messages[0], messages[1]]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        ConsumedApplicationMessage replayedMessage = await ReadNextAsync(channel);
        replayedMessage.ShouldBeSameAs(messages[0]);

        bool isRestartRequired = channel.Rollback([messages[0]]);

        isRestartRequired.ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);
        readMessages.ShouldBe(messages);
        messages.Select(message => message.ReplayCount).ShouldBe([2, 1, 0]);
    }

    [Fact]
    public async Task Rollback_ShouldReturnFalseAndReplayMessagesAfterRestart_WhenReadingIsCanceled()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);

        for (int i = 0; i < 2; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        Task<bool>[] tasks = [.. messages.Select(message => message.TaskCompletionSource.Task)];
        await channel.StopReadingAsync().WaitAsync(Timeout);
        channel.IsReadCanceled.ShouldBeTrue();

        bool isRestartRequired = channel.Rollback([messages[0]]);

        isRestartRequired.ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);
        channel.IsRestartPending.ShouldBeFalse();
        (await tasks[0].WaitAsync(Timeout)).ShouldBeFalse();
        (await tasks[1].WaitAsync(Timeout)).ShouldBeFalse();
        tasks[2].IsCompleted.ShouldBeFalse();

        channel.StartReading().ShouldBeTrue();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);
        readMessages.ShouldBe(messages);
        messages.Select(message => message.ReplayCount).ShouldBe([1, 1, 0]);
    }

    [Fact]
    public async Task Rollback_ShouldNotReplayOlderTrackedMessages_WhenTheyAreStillPending()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 4);

        // The first batch (messages 0 and 1) is still being committed (e.g. skipped) while the next batch (message 2) is rolled back
        for (int i = 0; i < 3; i++)
        {
            await ReadNextAsync(channel);
            channel.MarkTracked(messages[i]);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Tracked);
        }

        Task<bool>[] tasks = [.. messages.Select(message => message.TaskCompletionSource.Task)];

        bool isRestartRequired = channel.Rollback([messages[2]]);

        isRestartRequired.ShouldBeTrue();
        tasks[0].IsCompleted.ShouldBeFalse();
        tasks[1].IsCompleted.ShouldBeFalse();
        (await tasks[2].WaitAsync(Timeout)).ShouldBeFalse();
        tasks[3].IsCompleted.ShouldBeFalse();
        messages.Select(message => message.IsReplayPending).ShouldBe([false, false, true, false]);

        channel.RemoveRestartRequests(1).ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe([messages[2], messages[3]]);
        messages.Select(message => message.ReplayCount).ShouldBe([0, 0, 1, 0]);
    }

    [Fact]
    public async Task Rollback_ShouldNotReplayMessagesBeingAcknowledged()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);

        for (int i = 0; i < 3; i++)
        {
            await ReadNextAsync(channel);
            channel.MarkTracked(messages[i]);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Tracked);
        }

        // The second message was committed with another sequence and is being acknowledged when the first one is rolled back
        channel.BeginAcknowledgment(messages[1]).ShouldBeTrue();
        Task<bool> acknowledgingTask = messages[1].TaskCompletionSource.Task;

        channel.Rollback([messages[0]]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();

        messages.Select(message => message.IsReplayPending).ShouldBe([true, false, true]);
        acknowledgingTask.IsCompleted.ShouldBeFalse();

        channel.Acknowledged(messages[1]);
        (await acknowledgingTask.WaitAsync(Timeout)).ShouldBeTrue();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe([messages[0], messages[2]]);
    }

    [Fact]
    public async Task BeginAcknowledgment_ShouldReturnFalse_WhenMessageIsScheduledForReplay()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);

        for (int i = 0; i < 2; i++)
        {
            await ReadNextAsync(channel);
            channel.MarkTracked(messages[i]);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Tracked);
        }

        // The second message is committed by a sequence started before the reader is restarted to replay the first one: it's replayed
        // (and committed again) in order, instead of being acknowledged first
        channel.Rollback([messages[0]]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();

        channel.BeginAcknowledgment(messages[1]).ShouldBeFalse();
        messages[1].IsAcknowledging.ShouldBeFalse();

        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 2);
        readMessages.ShouldBe(messages);
        channel.BeginAcknowledgment(messages[1]).ShouldBeTrue();
    }

    [Fact]
    public async Task IsPending_ShouldReturnFalse_WhenChannelIsResetOrMessageIsAcknowledged()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);
        await ReadMessagesAsync(channel, 2);

        channel.IsPending(messages[0]).ShouldBeTrue();
        channel.Acknowledged(messages[0]);
        channel.IsPending(messages[0]).ShouldBeFalse();

        channel.IsPending(messages[1]).ShouldBeTrue();
        channel.Reset();
        channel.IsPending(messages[1]).ShouldBeFalse();
    }

    [Fact]
    public async Task Rollback_ShouldIgnoreMessages_WhenChannelIsReset()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage staleMessage = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(staleMessage, out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        Task<bool> staleTask = staleMessage.TaskCompletionSource.Task;

        channel.Reset();

        channel.Rollback([staleMessage]).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);
        staleTask.IsCompleted.ShouldBeFalse();
        staleMessage.IsReplayPending.ShouldBeFalse();

        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 2);
        await ReadNextAsync(channel);
        channel.Defer(message, out _).ShouldBe(ConsumerChannel.PendingState.Untracked);

        channel.Rollback([staleMessage, message]).ShouldBeTrue();

        staleTask.IsCompleted.ShouldBeFalse();
        staleMessage.IsReplayPending.ShouldBeFalse();
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);
        readMessage.ShouldBeSameAs(message);
        message.ReplayCount.ShouldBe(1);
        staleMessage.ReplayCount.ShouldBe(0);
    }

    [Fact]
    public void Rollback_ShouldReturnFalse_WhenNoMessagesAreSpecified()
    {
        using ConsumerChannel channel = CreateChannel();

        bool isRestartRequired = channel.Rollback([]);

        isRestartRequired.ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);
    }

    [Fact]
    public async Task RemoveRestartRequests_ShouldNotGoBelowZero_WhenSameRequestIsRemovedTwice()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 2);

        for (int i = 0; i < 2; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        channel.Rollback([messages[1]]).ShouldBeTrue();

        // Handled by two restarts (e.g. one delayed until after a stop and a start)
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);

        // The next rollback still requests a restart
        channel.Rollback([messages[0]]).ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.IsRestartPending.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRestartRequestsCount_ShouldCountRestartRequests()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);

        for (int i = 0; i < 3; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        channel.GetRestartRequestsCount().ShouldBe(0);
        channel.IsRestartPending.ShouldBeFalse();

        channel.Rollback([messages[2]]).ShouldBeTrue();
        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.IsRestartPending.ShouldBeTrue();

        channel.Rollback([messages[1]]).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(2);

        channel.Rollback([messages[0]]).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(3);
        channel.IsRestartPending.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveRestartRequests_ShouldReturnWhetherFurtherRequestsArePending()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);
        await ReadMessagesAsync(channel, 2);
        channel.Rollback([messages[0], messages[1]]).ShouldBeTrue();
        channel.Rollback([messages[0]]).ShouldBeFalse();

        channel.RemoveRestartRequests(1).ShouldBeTrue();

        channel.GetRestartRequestsCount().ShouldBe(1);
        channel.IsRestartPending.ShouldBeTrue();
        await Should.ThrowAsync<OperationCanceledException>(ReadNextAsync(channel));

        channel.RemoveRestartRequests(1).ShouldBeFalse();

        channel.GetRestartRequestsCount().ShouldBe(0);
        channel.IsRestartPending.ShouldBeFalse();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);
        readMessages.ShouldBe(messages);
    }

    [Fact]
    public async Task ClearRestartRequests_ShouldDiscardRequestsAndKeepMessagesToReplay()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 3);

        for (int i = 0; i < 2; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        channel.Rollback([messages[1]]).ShouldBeTrue();
        channel.Rollback([messages[0]]).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(2);

        channel.ClearRestartRequests();

        channel.GetRestartRequestsCount().ShouldBe(0);
        channel.IsRestartPending.ShouldBeFalse();
        ConsumedApplicationMessage[] readMessages = await ReadMessagesAsync(channel, 3);
        readMessages.ShouldBe(messages);
        messages.Select(message => message.ReplayCount).ShouldBe([1, 1, 0]);
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnTrue_WhenMessageIsDeferredAndUntracked()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeTrue();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnFalse_WhenMessageIsMarkedTracked()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.MarkTracked(message);

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnFalse_WhenMessageIsAcknowledged()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.Acknowledged(message);

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnFalse_WhenAcknowledgmentFailed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.FailAcknowledgment(message, new InvalidOperationException("test")).ShouldBeFalse();

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnFalse_WhenMessageIsRolledBack()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.Rollback([message]).ShouldBeTrue();

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnFalse_WhenReplayIsPending()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.PutBack(message);

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldCheckReplayCount_WhenMessageIsReplayed()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int firstReplayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.Rollback([message]).ShouldBeTrue();
        channel.RemoveRestartRequests(1).ShouldBeFalse();
        await ReadNextAsync(channel);
        channel.Defer(message, out int secondReplayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);

        bool isFirstAttemptStillUntracked = channel.IsStillUntracked(message, firstReplayCount);
        bool isSecondAttemptStillUntracked = channel.IsStillUntracked(message, secondReplayCount);

        firstReplayCount.ShouldBe(0);
        secondReplayCount.ShouldBe(1);
        isFirstAttemptStillUntracked.ShouldBeFalse();
        isSecondAttemptStillUntracked.ShouldBeTrue();
    }

    [Fact]
    public async Task IsStillUntracked_ShouldReturnFalse_WhenChannelIsReset()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage message = await WriteMessageAsync(channel, 1);
        await ReadNextAsync(channel);
        channel.Defer(message, out int replayCount).ShouldBe(ConsumerChannel.PendingState.Untracked);
        channel.Reset();

        bool isStillUntracked = channel.IsStillUntracked(message, replayCount);

        isStillUntracked.ShouldBeFalse();
    }

    [Fact]
    public async Task Reset_ShouldDiscardPendingBufferedAndReplayedMessages()
    {
        using ConsumerChannel channel = CreateChannel();
        ConsumedApplicationMessage[] messages = await WriteMessagesAsync(channel, 4);

        for (int i = 0; i < 2; i++)
        {
            await ReadNextAsync(channel);
            channel.Defer(messages[i], out _).ShouldBe(ConsumerChannel.PendingState.Untracked);
        }

        await ReadNextAsync(channel);
        channel.PutBack(messages[2]);

        channel.Reset();

        messages.ShouldAllBe(message => message.PendingNode == null);
        channel.FailAcknowledgment(messages[2], new InvalidOperationException("test")).ShouldBeFalse();
        channel.Rollback([messages[0], messages[1]]).ShouldBeFalse();
        channel.GetRestartRequestsCount().ShouldBe(0);

        ConsumedApplicationMessage newMessage = await WriteMessageAsync(channel, 5);
        ConsumedApplicationMessage readMessage = await ReadNextAsync(channel);
        readMessage.ShouldBeSameAs(newMessage);
        messages.ShouldAllBe(message => message.ReplayCount == 0);

        channel.PutBack(newMessage);
        ConsumedApplicationMessage replayedMessage = await ReadNextAsync(channel);
        replayedMessage.ShouldBeSameAs(newMessage);
        messages.ShouldAllBe(message => message.ReplayCount == 0);

        Task<ConsumedApplicationMessage> readTask = channel.ReadNextAsync().AsTask();
        readTask.IsCompleted.ShouldBeFalse();
        ConsumedApplicationMessage nextMessage = await WriteMessageAsync(channel, 6);
        (await readTask.WaitAsync(Timeout)).ShouldBeSameAs(nextMessage);
    }

    [Fact]
    public async Task StartReading_ShouldReplaceSequenceStoreWithoutDisposingIt_WhenReadingRestartsAfterStop()
    {
        using ConsumerChannel channel = CreateChannel();
        ISequenceStore initialSequenceStore = channel.SequenceStore;

        channel.StartReading().ShouldBeTrue();
        channel.SequenceStore.ShouldBeSameAs(initialSequenceStore);

        Task stop = channel.StopReadingAsync();
        await channel.NotifyReadingStoppedAsync(false);
        await stop.WaitAsync(Timeout);

        channel.StartReading().ShouldBeTrue();

        // The work still pending on the sequences of the stopped reader must neither affect the restarted one nor fail
        channel.SequenceStore.ShouldNotBeSameAs(initialSequenceStore);
        initialSequenceStore.IsDisposed.ShouldBeFalse();

        await channel.NotifyReadingStoppedAsync(false);
    }

    private static ConsumerChannel CreateChannel() => new(10, 0, Substitute.For<ISilverbackLogger>());

    private static async Task<ConsumedApplicationMessage> WriteMessageAsync(ConsumerChannel channel, long position)
    {
        ConsumedApplicationMessage message = new(
            new MqttApplicationMessageReceivedEventArgs(
                "client1",
                new MqttApplicationMessage
                {
                    Topic = "some-topic"
                },
                new MqttPublishPacket(),
                (_, _) => Task.CompletedTask),
            channel,
            position);

        await channel.WriteAsync(message, CancellationToken.None);

        return message;
    }

    private static async Task<ConsumedApplicationMessage[]> WriteMessagesAsync(ConsumerChannel channel, int count)
    {
        ConsumedApplicationMessage[] messages = new ConsumedApplicationMessage[count];

        for (int i = 0; i < count; i++)
        {
            messages[i] = await WriteMessageAsync(channel, i + 1);
        }

        return messages;
    }

    private static Task<ConsumedApplicationMessage> ReadNextAsync(ConsumerChannel channel) =>
        channel.ReadNextAsync().AsTask().WaitAsync(Timeout);

    private static async Task<ConsumedApplicationMessage[]> ReadMessagesAsync(ConsumerChannel channel, int count)
    {
        ConsumedApplicationMessage[] messages = new ConsumedApplicationMessage[count];

        for (int i = 0; i < count; i++)
        {
            messages[i] = await ReadNextAsync(channel);
        }

        return messages;
    }
}
