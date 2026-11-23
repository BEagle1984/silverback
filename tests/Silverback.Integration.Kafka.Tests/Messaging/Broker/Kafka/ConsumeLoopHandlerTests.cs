// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConsumeLoopHandlerTests
{
    private const string ConsumeLoopStartingEventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopStarting";

    private const string ConsumeLoopHandlerDisposingEventName = "Silverback.Integration.Kafka_Tracing_ConsumeLoopHandlerDisposing";

    private const string ConsumerReconnectTriggeredEventName = "Silverback.Integration_Tracing_ConsumerReconnectTriggered";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Start_ShouldNotStartAnotherLoop_WhenAlreadyConsuming()
    {
        using LoopFixture fixture = new();

        fixture.Handler.Start();
        Task stopping = fixture.Handler.Stopping;
        await fixture.WaitForPollsAsync(1);

        fixture.Handler.Start();

        fixture.Handler.IsConsuming.ShouldBeTrue();
        fixture.Handler.Stopping.ShouldBeSameAs(stopping);

        await fixture.Handler.StopAsync().WaitAsync(Timeout);

        stopping.IsCompleted.ShouldBeTrue();
        fixture.Logger.Count(ConsumeLoopStartingEventName).ShouldBe(1);
    }

    [Fact]
    public void WaitUntilCurrentPollCompletesAsync_ShouldReturnCompletedTask_WhenNotConsuming()
    {
        using LoopFixture fixture = new();

        Task result = fixture.Handler.WaitUntilCurrentPollCompletesAsync();

        result.ShouldBeSameAs(Task.CompletedTask);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The poll is released before the event is disposed.")]
    public async Task WaitUntilCurrentPollCompletesAsync_ShouldCompleteAllWaits_WhenPendingPollCompletes()
    {
        using LoopFixture fixture = new();
        TaskCompletionSource<bool> pollEntered = NewSignal();
        using ManualResetEventSlim releasePoll = new();
        fixture.EnqueuePoll(() =>
        {
            pollEntered.TrySetResult(true);
            releasePoll.Wait(Timeout);

            return null;
        });

        fixture.Handler.Start();
        await pollEntered.Task.WaitAsync(Timeout);

        Task firstWait = fixture.Handler.WaitUntilCurrentPollCompletesAsync();
        Task secondWait = fixture.Handler.WaitUntilCurrentPollCompletesAsync();

        firstWait.IsCompleted.ShouldBeFalse();
        secondWait.IsCompleted.ShouldBeFalse();

        releasePoll.Set();

        await Task.WhenAll(firstWait, secondWait).WaitAsync(Timeout);
        fixture.Handler.IsConsuming.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_ShouldDoNothing_WhenAlreadyDisposed()
    {
        using LoopFixture fixture = new();

        fixture.Handler.Dispose();
        fixture.Handler.Dispose();

        fixture.Logger.Count(ConsumeLoopHandlerDisposingEventName).ShouldBe(1);
        Should.Throw<ObjectDisposedException>(fixture.Handler.Start);
    }

    [Fact]
    public async Task Start_ShouldKeepPolling_WhenConsumeIsCanceledWhileNotStopping()
    {
        using LoopFixture fixture = new();
        OperationCanceledException exception = new("canceled poll");
        TaskCompletionSource<bool> nextPollStarted = NewSignal();
        fixture.EnqueuePoll(() => throw exception);
        fixture.EnqueuePoll(() =>
        {
            nextPollStarted.TrySetResult(true);

            return null;
        });

        fixture.Handler.Start();
        await nextPollStarted.Task.WaitAsync(Timeout);

        fixture.Handler.IsConsuming.ShouldBeTrue();
        fixture.Handler.Stopping.IsCompleted.ShouldBeFalse();
        fixture.Logger.Exceptions(KafkaLogEvents.ConsumingCanceled.EventId.Name).ShouldBe([exception]);
        fixture.Logger.Count(KafkaLogEvents.KafkaExceptionAutoRecovery.EventId.Name).ShouldBe(0);
        fixture.Logger.Count(KafkaLogEvents.KafkaExceptionNoAutoRecovery.EventId.Name).ShouldBe(0);
        await fixture.Client.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    public async Task Start_ShouldStopLoopAndDisconnectWithoutRecovery_WhenConsumeFailsAndAutoRecoveryIsDisabled()
    {
        using LoopFixture fixture = new(enableAutoRecovery: false);
        KafkaException exception = new(ErrorCode.Local_Fatal);
        fixture.EnqueuePoll(() => throw exception);

        fixture.Handler.Start();
        await fixture.Disconnected.Task.WaitAsync(Timeout);
        await fixture.Handler.Stopping.WaitAsync(Timeout);

        fixture.Logger.Exceptions(KafkaLogEvents.KafkaExceptionNoAutoRecovery.EventId.Name).ShouldBe([exception]);
        fixture.Logger.Count(KafkaLogEvents.KafkaExceptionAutoRecovery.EventId.Name).ShouldBe(0);
        fixture.Logger.Count(ConsumerReconnectTriggeredEventName).ShouldBe(0);
        await fixture.Client.Received(1).DisconnectAsync();
    }

    [Fact]
    public async Task Start_ShouldStopLoopAndTriggerReconnect_WhenNothingIsConsumedWithinStallDetectionThreshold()
    {
        using LoopFixture fixture = new(stallDetectionThreshold: TimeSpan.FromMilliseconds(1));

        fixture.Handler.Start();
        await fixture.Disconnected.Task.WaitAsync(Timeout);
        await fixture.Handler.Stopping.WaitAsync(Timeout);

        fixture.Logger.Count(KafkaLogEvents.StaleConsumer.EventId.Name).ShouldBe(1);
        await AsyncTestingUtil.WaitAsync(() => fixture.Logger.Count(ConsumerReconnectTriggeredEventName) > 0, Timeout);
        fixture.Logger.Count(ConsumerReconnectTriggeredEventName).ShouldBe(1);
        await fixture.Client.Received(1).DisconnectAsync();
    }

    [Fact]
    public async Task Start_ShouldKeepPolling_WhenStallDetectionThresholdIsNotExceeded()
    {
        using LoopFixture fixture = new(stallDetectionThreshold: TimeSpan.FromHours(1));

        fixture.Handler.Start();
        await fixture.WaitForPollsAsync(3);

        fixture.Handler.IsConsuming.ShouldBeTrue();
        fixture.Handler.Stopping.IsCompleted.ShouldBeFalse();
        fixture.Logger.Count(KafkaLogEvents.StaleConsumer.EventId.Name).ShouldBe(0);
        await fixture.Client.DidNotReceive().DisconnectAsync();
    }

    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class LoopFixture : IDisposable
    {
        private readonly BlockingCollection<Func<ConsumeResult<byte[]?, byte[]?>?>> _polls = [];

        private readonly ConsumerChannelsManager _channels;

        private int _pollsCount;

        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed with the fixture")]
        public LoopFixture(bool enableAutoRecovery = true, TimeSpan? stallDetectionThreshold = null)
        {
            Client = Substitute.For<IConfluentConsumerWrapper>();
            Client.Initialized.Returns(new AsyncEvent<BrokerClient>());
            Client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());

            Client.Consume(Arg.Any<TimeSpan>()).Returns(call =>
            {
                Interlocked.Increment(ref _pollsCount);

                return _polls.TryTake(out Func<ConsumeResult<byte[]?, byte[]?>?>? poll, call.Arg<TimeSpan>()) ? poll() : null;
            });

            Client.DisconnectAsync().Returns(_ =>
            {
                Disconnected.TrySetResult(true);

                return ValueTask.CompletedTask;
            });

            IBrokerClientCallbacksInvoker callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

            Consumer = new KafkaConsumer(
                "consumer",
                Client,
                new KafkaConsumerConfiguration
                {
                    GroupId = "tests",
                    EnableAutoRecovery = enableAutoRecovery,
                    StallDetectionThreshold = stallDetectionThreshold,
                    PollingTimeout = TimeSpan.FromMilliseconds(10),
                    Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                    [
                        new KafkaConsumerEndpointConfiguration
                        {
                            TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>([new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                        }
                    ])
                },
                Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
                callbacksInvoker,
                Substitute.For<IKafkaOffsetStoreFactory>(),
                Substitute.For<IServiceProvider>(),
                Logger);

            _channels = new ConsumerChannelsManager(Consumer, callbacksInvoker, Logger);
            Handler = new ConsumeLoopHandler(Consumer, _channels, new OffsetsTracker(), Logger);
        }

        public IConfluentConsumerWrapper Client { get; }

        public KafkaConsumer Consumer { get; }

        public ConsumeLoopHandler Handler { get; }

        public CapturingLogger Logger { get; } = new();

        public TaskCompletionSource<bool> Disconnected { get; } = NewSignal();

        public void EnqueuePoll(Func<ConsumeResult<byte[]?, byte[]?>?> poll) => _polls.Add(poll);

        public async Task WaitForPollsAsync(int count)
        {
            using CancellationTokenSource timeout = new(Timeout);

            while (_pollsCount < count)
            {
                await Task.Delay(1, timeout.Token);
            }
        }

        public void Dispose()
        {
            Handler.Dispose();
            _channels.Dispose();
            Consumer.Dispose();
            _polls.Dispose();
        }
    }

    private sealed class CapturingLogger : ISilverbackLogger<KafkaConsumer>, ILogger
    {
        private readonly ConcurrentQueue<(string? EventName, Exception? Exception)> _entries = new();

        public ILogger InnerLogger => this;

        public int Count(string? eventName) => _entries.Count(entry => entry.EventName == eventName);

        public Exception?[] Exceptions(string? eventName) =>
            [.. _entries.Where(entry => entry.EventName == eventName).Select(entry => entry.Exception)];

        public bool IsEnabled(LogEvent logEvent) => true;

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _entries.Enqueue((eventId.Name, exception));
    }
}
