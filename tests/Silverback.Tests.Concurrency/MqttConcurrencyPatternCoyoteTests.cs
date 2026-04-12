// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Concurrency;

// MQTT-specific concurrency tests targeting bugs found in the MQTT integration.
//
// All tests are pattern-level: they extract the production code's synchronization
// pattern into standalone code that Coyote can schedule. The MQTT classes
// (ConsumerChannelsManager, MqttClientWrapper) are internal with complex
// dependencies, so pattern tests are the pragmatic choice.
//
// MC1: _nextChannelIndex++ non-atomic increment in OnMessageReceivedAsync
// MC2: _publishQueueChannel recreation race (check-then-create)
// MC3: _pendingReconnect / _mqttClientWasConnected TOCTOU on non-volatile bools
// MC4: channel.Reset() racing with OnMessageReceivedAsync after reconnect
public class MqttConcurrencyPatternCoyoteTests
{
    private readonly ITestOutputHelper _output;

    public MqttConcurrencyPatternCoyoteTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // MC1 — _nextChannelIndex++ non-atomic increment.
    //
    // ConsumerChannelsManager.OnMessageReceivedAsync (line 84):
    //   ConsumerChannel channel = _channels[_nextChannelIndex++];
    //
    // MQTTnet fires OnMessageReceivedAsync on arbitrary ThreadPool threads.
    // Two concurrent callbacks can both read the same index value, route
    // messages to the same channel, and lose an increment. With N channels,
    // some channels get double-fed while others starve.
    //
    // Additionally line 92 has a redundant modular write:
    //   _nextChannelIndex = (_nextChannelIndex + 1) % _channels.Length;
    // which compounds the race (the ++ at line 84 and the modular write at
    // line 92 both modify the same field).
    [Fact]
    public void NextChannelIndex_ConcurrentIncrements_ShouldDistributeEvenly()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                const int channelCount = 4;
                int nextChannelIndex = 0;
                int[] channelHits = new int[channelCount];

                // Mirrors OnMessageReceivedAsync: read index, route, increment.
                // Production code: _channels[_nextChannelIndex++]
                // then: _nextChannelIndex = (_nextChannelIndex + 1) % _channels.Length
                void RouteMessage()
                {
                    int index = nextChannelIndex; // non-atomic read
                    Interlocked.Increment(ref channelHits[index % channelCount]);
                    nextChannelIndex = (index + 1) % channelCount; // non-atomic write
                }

                // Simulate 8 concurrent MQTTnet message-received callbacks
                Task[] callbacks = Enumerable.Range(0, 8)
                    .Select(_ => Task.Run(RouteMessage))
                    .ToArray();

                await Task.WhenAll(callbacks).ConfigureAwait(false);

                // Invariant: total hits must equal total messages.
                int totalHits = channelHits.Sum();
                totalHits.ShouldBe(8, "Lost message routing due to _nextChannelIndex race.");

                // Stricter: no channel should get 0 hits if messages >= channels.
                // With 8 messages and 4 channels, each channel should get 2 in the
                // non-racy case. Any channel with 0 hits means the round-robin broke.
                for (int i = 0; i < channelCount; i++)
                {
                    channelHits[i].ShouldBeGreaterThan(
                        0,
                        $"Channel {i} received 0 messages out of 8. " +
                        "The non-atomic _nextChannelIndex++ in OnMessageReceivedAsync " +
                        "caused multiple callbacks to read the same index, skipping " +
                        "this channel entirely. See MC1.");
                }
            },
            _output);
    }

    // MC2 — _publishQueueChannel recreation race.
    //
    // MqttClientWrapper.ConnectCoreAsync (lines 113-114):
    //   if (_publishQueueChannel.Reader.Completion.IsCompleted)
    //       _publishQueueChannel = Channel.CreateUnbounded<QueuedMessage>();
    //
    // If ProcessPublishQueueAsync is still draining the old channel while
    // ConnectCoreAsync recreates it, the old channel reference held by the
    // drainer becomes stale. Messages written to the new channel are never
    // read; messages the drainer reads from the old channel are for the
    // previous session. No synchronization between disconnect + reconnect.
    [Fact]
    public void PublishQueueChannel_RecreationDuringDrain_ShouldNotLoseMessages()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                Channel<string> publishChannel = Channel.CreateUnbounded<string>();
                ConcurrentBag<string> drained = new();

                // T1: drainer (mirrors ProcessPublishQueueAsync) — holds old channel ref
                Channel<string> drainerRef = publishChannel;
                Task drainer = Task.Run(
                    async () =>
                    {
                        await foreach (string msg in drainerRef.Reader.ReadAllAsync().ConfigureAwait(false))
                        {
                            drained.Add(msg);
                            await Task.Yield();
                        }
                    });

                // Write a message to the current channel
                await publishChannel.Writer.WriteAsync("msg-1").ConfigureAwait(false);
                await Task.Yield();

                // T2: reconnect path — check-then-recreate (mirrors ConnectCoreAsync)
                Task reconnect = Task.Run(
                    () =>
                    {
                        // This is the racy pattern: check completion, then replace
                        if (!publishChannel.Reader.Completion.IsCompleted)
                        {
                            publishChannel.Writer.Complete(); // complete old
                        }

                        publishChannel = Channel.CreateUnbounded<string>(); // replace
                    });

                await reconnect.ConfigureAwait(false);

                // Write to the NEW channel — drainer still reads from the OLD one
                await publishChannel.Writer.WriteAsync("msg-2").ConfigureAwait(false);
                publishChannel.Writer.Complete();

                // Give drainer time to finish the old channel
                await drainer.ConfigureAwait(false);

                // Invariant: drainer should have seen ALL messages. But msg-2 was written
                // to the new channel while the drainer holds the old reference.
                drained.Count.ShouldBe(
                    2,
                    $"Drainer only saw {drained.Count}/2 messages. " +
                    "The channel was recreated without redirecting the drainer to the " +
                    "new instance. Messages written after reconnect are lost. See MC2.");
            },
            _output);
    }

    // MC3 — _pendingReconnect / _mqttClientWasConnected TOCTOU.
    //
    // MqttClientWrapper.TryConnectAsync (lines 168-183):
    //   if (_mqttClientWasConnected)      // non-volatile read
    //   {
    //       _pendingReconnect = true;     // non-volatile write
    //       _mqttClientWasConnected = false;
    //   }
    //   if (_pendingReconnect)            // non-volatile read
    //   {
    //       _pendingReconnect = false;    // non-volatile write
    //   }
    //
    // These bools are read/written from the background ConnectAndKeepConnectionAlive
    // task and can be indirectly affected by disconnect events on the MQTTnet thread.
    // Two concurrent calls to TryConnectAsync can both read _mqttClientWasConnected=true
    // and both set _pendingReconnect=true, then both read _pendingReconnect=true and
    // both execute the reconnect logic.
    [Fact]
    public void PendingReconnect_ConcurrentTryConnect_ShouldReconnectExactlyOnce()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                bool mqttClientWasConnected = true;
                bool pendingReconnect = false;
                int reconnectCount = 0;

                Task TryConnect()
                {
                    // Mirrors TryConnectAsync lines 168-183
                    if (mqttClientWasConnected)
                    {
                        pendingReconnect = true;
                        mqttClientWasConnected = false;
                    }

                    if (pendingReconnect)
                    {
                        pendingReconnect = false;
                        Interlocked.Increment(ref reconnectCount);
                    }

                    return Task.CompletedTask;
                }

                Task t1 = Task.Run(TryConnect);
                Task t2 = Task.Run(TryConnect);

                await Task.WhenAll(t1, t2).ConfigureAwait(false);

                reconnectCount.ShouldBe(
                    1,
                    $"Reconnect executed {reconnectCount} times instead of 1. " +
                    "Two concurrent TryConnectAsync calls both read " +
                    "_mqttClientWasConnected=true (non-volatile) and both triggered " +
                    "the reconnect path. See MC3.");
            },
            _output);
    }

    // MC4 — channel.Reset() racing with OnMessageReceivedAsync.
    //
    // ConsumerChannelsManager.OnMessageReceivedAsync (lines 87-88):
    //   if (channel.IsCompleted)
    //       channel.Reset();
    //
    // After a reconnect, channels may be marked completed. The next message
    // callback checks IsCompleted and calls Reset(). But another callback
    // may also see IsCompleted=true for the same channel and call Reset()
    // concurrently, or a third callback may try to Write to a channel that
    // is mid-Reset.
    [Fact]
    public void ChannelReset_ConcurrentWithWrite_ShouldNotLoseMessages()
    {
        CoyoteTestRunner.Run(
            async () =>
            {
                Channel<string> channel = Channel.CreateBounded<string>(10);
                channel.Writer.Complete(); // simulate "completed after disconnect"
                bool isCompleted = true;
                ConcurrentBag<string> written = new();

                async Task OnMessageReceived(string message)
                {
                    // Mirrors OnMessageReceivedAsync lines 87-92
                    if (isCompleted)
                    {
                        channel = Channel.CreateBounded<string>(10); // Reset
                        isCompleted = false;
                    }

                    bool success = channel.Writer.TryWrite(message);
                    if (success)
                        written.Add(message);

                    await Task.Yield();
                }

                // Two callbacks racing after reconnect
                Task t1 = Task.Run(async () => await OnMessageReceived("msg-1").ConfigureAwait(false));
                Task t2 = Task.Run(async () => await OnMessageReceived("msg-2").ConfigureAwait(false));

                await Task.WhenAll(t1, t2).ConfigureAwait(false);

                written.Count.ShouldBe(
                    2,
                    $"Only {written.Count}/2 messages written after reconnect. " +
                    "Concurrent OnMessageReceivedAsync callbacks both saw " +
                    "IsCompleted=true, both called Reset (creating two channels), " +
                    "and one message was written to a channel that was immediately " +
                    "replaced. See MC4.");
            },
            _output);
    }
}
