// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerChannelsManagerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task StartReading_ShouldNotifyReadingStoppedOnce_WhenReadingThrowsAndChannelIsRestartedWhileDisconnecting()
    {
        using ConsumerChannel<int> innerChannel = new(10, "test", Substitute.For<ISilverbackLogger>());
        RecordingChannel channel = new(innerChannel);

        // Created without RunContinuationsAsynchronously, so that the failed loop resumes synchronously when the disconnection completes
        TaskCompletionSource disconnection = new();
        TaskCompletionSource<bool> disconnecting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IConsumer consumer = Substitute.For<IConsumer>();
        consumer.Client.DisconnectAsync().Returns(_ =>
        {
            channel.Events.Enqueue("Disconnect");
            disconnecting.TrySetResult(true);
            return new ValueTask(disconnection.Task);
        });

        using FailingOnceConsumerChannelsManager manager = new(consumer, channel);

        try
        {
            manager.StartReading();
            await disconnecting.Task.WaitAsync(Timeout);

            // Like a reconnection or an assignment restarting the channel while the failed loop is disconnecting the client
            manager.StartReading();
            await manager.Restarted.Task.WaitAsync(Timeout);

            // The failed loop runs to its end within this call
            disconnection.SetResult();

            channel.Events.ShouldBe(["NotifyReadingStopped(True)", "Disconnect"]);
            channel.ReadTask.IsCompleted.ShouldBeFalse();

            await manager.StopReadingAsync().WaitAsync(Timeout);

            channel.Events.ShouldBe(["NotifyReadingStopped(True)", "Disconnect", "NotifyReadingStopped(False)"]);
        }
        finally
        {
            disconnection.TrySetResult();
        }
    }

    private sealed class RecordingChannel : IConsumerChannel
    {
        private readonly ConsumerChannel<int> _innerChannel;

        public RecordingChannel(ConsumerChannel<int> innerChannel)
        {
            _innerChannel = innerChannel;
        }

        public ConcurrentQueue<string> Events { get; } = new();

        public string Id => _innerChannel.Id;

        public CancellationToken ReadCancellationToken => _innerChannel.ReadCancellationToken;

        public Task ReadTask => _innerChannel.ReadTask;

        public void Complete() => _innerChannel.Complete();

        public void Reset() => _innerChannel.Reset();

        public bool StartReading() => _innerChannel.StartReading();

        public Task StopReadingAsync() => _innerChannel.StopReadingAsync();

        public Task NotifyReadingStoppedAsync(bool hasThrown)
        {
            Events.Enqueue($"NotifyReadingStopped({hasThrown})");
            return _innerChannel.NotifyReadingStoppedAsync(hasThrown);
        }

        public ValueTask<int> ReadAsync() => _innerChannel.ReadAsync();
    }

    private sealed class FailingOnceConsumerChannelsManager : ConsumerChannelsManager<RecordingChannel>
    {
        private readonly RecordingChannel _channel;

        private int _readCount;

        public FailingOnceConsumerChannelsManager(IConsumer consumer, RecordingChannel channel)
            : base(consumer, Substitute.For<ISilverbackLogger>())
        {
            _channel = channel;
        }

        public TaskCompletionSource<bool> Restarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void StartReading() => StartReading(_channel);

        protected override IEnumerable<RecordingChannel> GetChannels() => [_channel];

        // The first read fails, the next ones wait for a message until the reading is stopped
        protected override async Task ReadChannelOnceAsync(RecordingChannel channel)
        {
            if (Interlocked.Increment(ref _readCount) == 1)
                throw new InvalidOperationException("test");

            Restarted.TrySetResult(true);
            await channel.ReadAsync();
        }
    }
}
