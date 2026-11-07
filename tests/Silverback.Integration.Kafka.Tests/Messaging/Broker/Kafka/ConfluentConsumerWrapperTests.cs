// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentConsumerWrapperTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly IConfluentConsumerBuilder _consumerBuilder = Substitute.For<IConfluentConsumerBuilder>();

    private readonly IConsumer<byte[]?, byte[]?> _confluentConsumer = Substitute.For<IConsumer<byte[]?, byte[]?>>();

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    private readonly IConfluentAdminClientFactory _adminClientFactory = Substitute.For<IConfluentAdminClientFactory>();

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory = Substitute.For<IKafkaOffsetStoreFactory>();

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private readonly ISilverbackLogger _logger = Substitute.For<ISilverbackLogger>();

    private readonly LoggerSubstitute<ConfluentConsumerWrapperTests> _loggerSubstitute = new(LogLevel.Trace);

    public ConfluentConsumerWrapperTests()
    {
        _logger.InnerLogger.Returns(_loggerSubstitute);
        _logger.IsEnabled(Arg.Any<LogEvent>()).Returns(true);

        _consumerBuilder.SetConfig(Arg.Any<ConsumerConfig>()).Returns(_consumerBuilder);
        _consumerBuilder.SetErrorHandler(Arg.Any<Action<IConsumer<byte[]?, byte[]?>, Error>>()).Returns(_consumerBuilder);
        _consumerBuilder.Build().Returns(_confluentConsumer);
        _confluentConsumer.Assignment.Returns(_ => []);
    }

    [Fact]
    public async Task ConnectAsync_ShouldSubscribeTopics()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", Partition.Any, Offset.Beginning),
                        new TopicPartitionOffset("topic2", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);

        await consumer.ConnectAsync();

        _confluentConsumer.Received(1).Subscribe(
            Arg.Is<IEnumerable<string>>(enumerable =>
                enumerable.SequenceEqual(new[] { "topic1", "topic2" })));
    }

    [Fact]
    public async Task ConnectAsync_ShouldSubscribeWithRegex()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("^test_[0-9]*", Partition.Any, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);

        await consumer.ConnectAsync();

        _confluentConsumer.Received(1).Subscribe(
            Arg.Is<IEnumerable<string>>(enumerable =>
                enumerable.SequenceEqual(new[] { "^test_[0-9]*" })));
    }

    [Fact]
    public async Task ConnectAsync_ShouldAssignSpecificPartitions()
    {
        KafkaConsumerConfiguration configuration = new()
        {
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic1", 1, Offset.Beginning),
                        new TopicPartitionOffset("topic1", 2, 42),
                        new TopicPartitionOffset("topic2", 3, Offset.Beginning)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };

        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);

        consumer.Consumer = GetKafkaConsumer(consumer);

        await consumer.ConnectAsync();

        _confluentConsumer.Received(1).Assign(
            Arg.Is<IEnumerable<TopicPartitionOffset>>(enumerable =>
                enumerable.SequenceEqual(
                    new[]
                    {
                        new TopicPartitionOffset("topic1", 1, Offset.Beginning),
                        new TopicPartitionOffset("topic1", 2, 42),
                        new TopicPartitionOffset("topic2", 3, Offset.Beginning)
                    })));
    }

    [Fact]
    public async Task StoreOffset_ShouldStoreLocallyWithoutCommitting_WhenOffsetsCommitIsDisabled()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync(commitOffsets: false);
        TopicPartitionOffset offset = new("topic", 0, 2);

        consumer.StoreOffset(offset);
        consumer.Commit();
        await consumer.DisconnectAsync();

        _confluentConsumer.Received(1).StoreOffset(offset);
        _confluentConsumer.DidNotReceive().Commit();
    }

    [Theory]
    [InlineData(ErrorCode.UnknownMemberId)]
    [InlineData(ErrorCode.Local_TimedOut)]
    [InlineData(ErrorCode.GroupAuthorizationFailed)]
    public async Task Commit_ShouldLogAndNotifyCallback_WhenKafkaExceptionIsThrown(ErrorCode errorCode)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        KafkaException exception = new(new Error(errorCode));
        TopicPartitionOffset offset = new("topic", 0, 2);
        IKafkaOffsetCommittedCallback callback = Substitute.For<IKafkaOffsetCommittedCallback>();
        _confluentConsumer.Commit().Returns(_ => throw exception, _ => [offset]);

        _callbacksInvoker.When(invoker => invoker.Invoke(Arg.Any<Action<IKafkaOffsetCommittedCallback>>()))
            .Do(call => call.Arg<Action<IKafkaOffsetCommittedCallback>>()(callback));

        Action act = consumer.Commit;

        act.ShouldNotThrow();
        _loggerSubstitute.Received(LogLevel.Error, typeof(KafkaException), eventId: 1033, exceptionMessage: exception.Message);
        callback.Received(1).OnOffsetsCommitted(
            Arg.Is<CommittedOffsets>(offsets => offsets.Error.Code == errorCode),
            consumer.Consumer);

        consumer.Commit();

        callback.Received(1).OnOffsetsCommitted(
            Arg.Is<CommittedOffsets>(offsets => !offsets.Error.IsError && offsets.Offsets.Single().TopicPartitionOffset == offset),
            consumer.Consumer);
    }

    [Fact]
    public async Task Commit_ShouldLogAndNotifyCallback_WhenPartitionCommitFails()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        TopicPartitionOffsetException exception = new(
        [
            new TopicPartitionOffsetError(new TopicPartitionOffset("topic", 0, 1), new Error(ErrorCode.UnknownMemberId))
        ]);
        IKafkaOffsetCommittedCallback callback = Substitute.For<IKafkaOffsetCommittedCallback>();
        _confluentConsumer.Commit().Returns(_ => throw exception);

        _callbacksInvoker.When(invoker => invoker.Invoke(Arg.Any<Action<IKafkaOffsetCommittedCallback>>()))
            .Do(call => call.Arg<Action<IKafkaOffsetCommittedCallback>>()(callback));

        Action act = consumer.Commit;

        act.ShouldNotThrow();
        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Error occurred committing offset topic[0]@1: 'Broker: Unknown member' (25) | ConsumerName: test",
            2038);
        callback.Received(1).OnOffsetsCommitted(
            Arg.Is<CommittedOffsets>(offsets => offsets.Offsets.Single().Error.Code == ErrorCode.UnknownMemberId),
            consumer.Consumer);
    }

    [Fact]
    public async Task Commit_ShouldNotThrow_WhenNoOffsetIsStored()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        _confluentConsumer.Commit().Returns(_ => throw new KafkaException(new Error(ErrorCode.Local_NoOffset)));

        Action act = consumer.Commit;

        act.ShouldNotThrow();
        _loggerSubstitute.DidNotReceive(LogLevel.Error, typeof(KafkaException));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The late call completes before the consumer is disposed")]
    public async Task StoreOffsetAndCommit_ShouldThrowWithoutCallingClient_WhenClientIsReleased(bool commit)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        TopicPartitionOffset offset = new("topic", 0, 2);
        Action act = commit ? consumer.Commit : () => consumer.StoreOffset(offset);
        Task lateCall = Task.CompletedTask;
        bool lateCallCompleted = false;

        // A late call from a processing thread racing the release of the native consumer
        _confluentConsumer.When(native => native.Dispose()).Do(_ =>
        {
            lateCall = Task.Run(act);
            lateCallCompleted = SpinWait.SpinUntil(() => lateCall.IsCompleted, Timeout);
        });

        await consumer.DisconnectAsync().AsTask().WaitAsync(Timeout);

        lateCallCompleted.ShouldBeTrue();
        await Should.ThrowAsync<InvalidOperationException>(lateCall);
        act.ShouldThrow<InvalidOperationException>();
        _confluentConsumer.DidNotReceive().StoreOffset(Arg.Any<TopicPartitionOffset>());
        _confluentConsumer.DidNotReceive().Commit();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisconnectAsync_ShouldCloseAndDisposeClient_WhenFinalCommitFails(bool partitionError)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync(false);
        KafkaException exception = partitionError
            ? new TopicPartitionOffsetException(
            [
                new TopicPartitionOffsetError(new TopicPartitionOffset("topic", 0, 1), new Error(ErrorCode.UnknownMemberId))
            ])
            : new KafkaException(new Error(ErrorCode.UnknownMemberId));
        _confluentConsumer.Commit().Returns(_ => throw exception);

        await consumer.DisconnectAsync();

        consumer.Status.ShouldBe(ClientStatus.Disconnected);
        _confluentConsumer.Received(1).Close();
        _confluentConsumer.Received(1).Dispose();

        // Repeated cleanup must not commit or dispose the released native client again
        await consumer.DisconnectAsync();

        _confluentConsumer.Received(1).Commit();
        _confluentConsumer.Received(1).Dispose();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The assertion delegate is awaited before the consumer is disposed")]
    public async Task DisconnectAsync_ShouldDisposeClient_WhenCloseFails()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        KafkaException exception = new(new Error(ErrorCode.Local_Fail));
        _confluentConsumer.When(native => native.Close()).Do(_ => throw exception);

        Func<Task> act = () => consumer.DisconnectAsync().AsTask();

        (await act.ShouldThrowAsync<KafkaException>()).ShouldBeSameAs(exception);
        _confluentConsumer.Received(1).Dispose();

        await consumer.DisconnectAsync();

        _confluentConsumer.Received(1).Close();
        _confluentConsumer.Received(1).Dispose();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldCloseAndDisposeClient_WhenNoOffsetIsStored()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync(false);
        _confluentConsumer.Commit().Returns(_ => throw new KafkaException(new Error(ErrorCode.Local_NoOffset)));

        await consumer.DisconnectAsync();

        consumer.Status.ShouldBe(ClientStatus.Disconnected);
        _confluentConsumer.Received(1).Close();
        _confluentConsumer.Received(1).Dispose();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are awaited before the consumer is disposed")]
    public async Task DisconnectAsync_ShouldWaitForPendingStoreOffset_BeforeClosingClient()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        TopicPartitionOffset offset = new("topic", 0, 2);
        ConcurrentQueue<string> nativeCalls = [];
        TaskCompletionSource storeOffsetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseStoreOffset = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource disconnecting = new(TaskCreationOptions.RunContinuationsAsynchronously);

        _confluentConsumer.When(native => native.StoreOffset(offset)).Do(_ =>
        {
            storeOffsetStarted.TrySetResult();
            releaseStoreOffset.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
            nativeCalls.Enqueue("StoreOffset");
        });

        _confluentConsumer.When(native => native.Pause(Arg.Any<IEnumerable<TopicPartition>>())).Do(_ => nativeCalls.Enqueue("Pause"));
        _confluentConsumer.When(native => native.Unsubscribe()).Do(_ => nativeCalls.Enqueue("Unsubscribe"));
        _confluentConsumer.When(native => native.Close()).Do(_ => nativeCalls.Enqueue("Close"));
        _confluentConsumer.When(native => native.Dispose()).Do(_ => nativeCalls.Enqueue("Dispose"));

        consumer.Disconnecting.AddHandler(_ =>
        {
            disconnecting.TrySetResult();
            return ValueTask.CompletedTask;
        });

        Task storeOffsetTask = Task.Run(() => consumer.StoreOffset(offset));
        await storeOffsetStarted.Task.WaitAsync(Timeout);

        Task disconnectTask = Task.Run(() => consumer.DisconnectAsync().AsTask());
        await disconnecting.Task.WaitAsync(Timeout);
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        nativeCalls.ShouldBeEmpty();
        disconnectTask.IsCompleted.ShouldBeFalse();

        releaseStoreOffset.SetResult();
        await Task.WhenAll(storeOffsetTask, disconnectTask).WaitAsync(Timeout);

        nativeCalls.ShouldBe(["StoreOffset", "Pause", "Unsubscribe", "Close", "Dispose"]);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The late call completes before the consumer is disposed")]
    public async Task DisconnectAsync_ShouldNotDeadlock_WhenCallbacksWaitForStoreOffsetOrCommitFromAnotherThread(bool commit, bool leavingGroup)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        TopicPartitionOffset offset = new("topic", 0, 2);
        Action act = commit ? consumer.Commit : () => consumer.StoreOffset(offset);
        ConcurrentQueue<string> nativeCalls = [];
        Task lateCall = Task.CompletedTask;
        bool lateCallCompleted = false;

        // Like the revocation callbacks waiting for the processing, which stores and commits the offsets. They run in the polls while
        // leaving the group, or in Close.
        void RunLateCall()
        {
            lateCall = Task.Run(act);
            lateCallCompleted = SpinWait.SpinUntil(() => lateCall.IsCompleted, Timeout);
        }

        _confluentConsumer.When(native => native.Unsubscribe()).Do(_ => nativeCalls.Enqueue("Unsubscribe"));

        if (leavingGroup)
        {
            _confluentConsumer.When(native => native.Consume(Arg.Any<TimeSpan>())).Do(_ =>
            {
                // The consume loop polls until the disconnect stops it
                if (!nativeCalls.Contains("Unsubscribe") || nativeCalls.Contains("LateCall"))
                    return;

                nativeCalls.Enqueue("LateCall");
                RunLateCall();
            });
        }
        else
        {
            _confluentConsumer.When(native => native.Close()).Do(_ => RunLateCall());
        }

        await consumer.DisconnectAsync().AsTask().WaitAsync(Timeout);

        lateCallCompleted.ShouldBeTrue();
        await Should.ThrowAsync<InvalidOperationException>(lateCall);
        _confluentConsumer.DidNotReceive().StoreOffset(Arg.Any<TopicPartitionOffset>());
        _confluentConsumer.DidNotReceive().Commit();
        _confluentConsumer.Received(1).Close();
        _confluentConsumer.Received(1).Dispose();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The callbacks run before the consumer is disposed")]
    public async Task DisconnectAsync_ShouldLeaveGroupAndRunPendingCallbacks_BeforeClosing()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        ConcurrentQueue<string> nativeCalls = [];
        List<TopicPartition> assignment = [new("topic", 0)];

        _confluentConsumer.Assignment.Returns(_ => assignment);
        _confluentConsumer.Commit().Returns(_ => []);

        _confluentConsumer.When(native => native.Pause(Arg.Any<IEnumerable<TopicPartition>>())).Do(call =>
        {
            IEnumerable<int> partitions = call.Arg<IEnumerable<TopicPartition>>().Select(partition => partition.Partition.Value);
            nativeCalls.Enqueue($"Pause {string.Join(',', partitions)}");
        });

        _confluentConsumer.When(native => native.Unsubscribe()).Do(_ => nativeCalls.Enqueue("Unsubscribe"));
        _confluentConsumer.When(native => native.Close()).Do(_ => nativeCalls.Enqueue("Close"));

        // A pending assignment, a prefetched record and then the revocation following the unsubscribe
        _confluentConsumer.Consume(Arg.Any<TimeSpan>()).Returns(_ =>
        {
            // The consume loop polls until the disconnect stops it
            if (!nativeCalls.Contains("Unsubscribe"))
                return null;

            nativeCalls.Enqueue("Consume");

            switch (nativeCalls.Count(call => call == "Consume"))
            {
                case 1:
                    assignment.Add(new TopicPartition("topic", 1));
                    return null;
                case 2:
                    return new ConsumeResult<byte[]?, byte[]?>();
                default:
                    // The revocation commits from the disconnecting thread
                    consumer.Commit();
                    assignment.Clear();
                    return null;
            }
        });

        await consumer.DisconnectAsync();

        nativeCalls.ShouldBe(["Pause 0", "Unsubscribe", "Consume", "Consume", "Consume", "Close"]);
        _confluentConsumer.Received(1).Consume(TimeSpan.Zero);
        _confluentConsumer.Received(1).Commit();
        _confluentConsumer.DidNotReceive().StoreOffset(Arg.Any<TopicPartitionOffset>());
        _confluentConsumer.DidNotReceive().StoreOffset(Arg.Any<ConsumeResult<byte[]?, byte[]?>>());
        consumer.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldCloseWithoutLeavingGroup_WhenPartitionsAreStaticallyAssigned()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync(staticAssignment: true);

        await consumer.DisconnectAsync();

        _confluentConsumer.DidNotReceive().Pause(Arg.Any<IEnumerable<TopicPartition>>());
        _confluentConsumer.DidNotReceive().Unsubscribe();
        _confluentConsumer.Received(1).Close();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldLogAndClose_WhenAssignmentIsNotReleasedWithinTimeout()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        consumer.LeaveGroupTimeout = TimeSpan.FromMilliseconds(100);
        _confluentConsumer.Assignment.Returns(_ => [new TopicPartition("topic", 0)]);

        await consumer.DisconnectAsync();

        _loggerSubstitute.Received(LogLevel.Warning, null, eventId: 2045);
        _confluentConsumer.Received(1).Close();
        consumer.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldLogAndLeaveGroup_WhenPauseFails()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        KafkaException exception = new(new Error(ErrorCode.Local_Fail));
        _confluentConsumer.When(native => native.Pause(Arg.Any<IEnumerable<TopicPartition>>())).Do(_ => throw exception);

        await consumer.DisconnectAsync();

        _loggerSubstitute.Received(LogLevel.Warning, typeof(KafkaException), eventId: 2044, exceptionMessage: exception.Message);
        _confluentConsumer.Received(1).Unsubscribe();
        _confluentConsumer.Received(1).Close();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldLogAndClose_WhenUnsubscribeFails()
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        KafkaException exception = new(new Error(ErrorCode.Local_Fail));
        ConcurrentQueue<string> nativeCalls = [];

        _confluentConsumer.When(native => native.Unsubscribe()).Do(_ =>
        {
            nativeCalls.Enqueue("Unsubscribe");
            throw exception;
        });

        // The consume loop polls until the disconnect stops it
        _confluentConsumer.When(native => native.Consume(Arg.Any<TimeSpan>())).Do(_ =>
        {
            if (nativeCalls.Contains("Unsubscribe"))
                nativeCalls.Enqueue("Consume");
        });

        _confluentConsumer.When(native => native.Close()).Do(_ => nativeCalls.Enqueue("Close"));

        await consumer.DisconnectAsync();

        _loggerSubstitute.Received(LogLevel.Warning, typeof(KafkaException), eventId: 2044, exceptionMessage: exception.Message);
        nativeCalls.ShouldBe(["Unsubscribe", "Close"]);
        consumer.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisconnectAsync_ShouldLogPollErrorAndStopLeavingGroupOnlyIfFatal(bool fatal)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        KafkaException exception = new(new Error(ErrorCode.Local_Fail, "poll failed", fatal));
        ConcurrentQueue<string> nativeCalls = [];
        List<TopicPartition> assignment = [new("topic", 0)];

        _confluentConsumer.Assignment.Returns(_ => assignment);
        _confluentConsumer.When(native => native.Unsubscribe()).Do(_ => nativeCalls.Enqueue("Unsubscribe"));
        _confluentConsumer.Consume(Arg.Any<TimeSpan>()).Returns<ConsumeResult<byte[]?, byte[]?>?>(_ =>
        {
            // The consume loop polls until the disconnect stops it
            if (!nativeCalls.Contains("Unsubscribe"))
                return null;

            nativeCalls.Enqueue("Consume");

            if (nativeCalls.Count(call => call == "Consume") == 1)
                throw exception;

            assignment.Clear();
            return null;
        });

        await consumer.DisconnectAsync();

        _loggerSubstitute.Received(LogLevel.Warning, typeof(KafkaException), eventId: 2044, exceptionMessage: exception.Message);
        nativeCalls.Count(call => call == "Consume").ShouldBe(fatal ? 1 : 2);
        _confluentConsumer.Received(1).Close();
    }

    private static KafkaConsumer GetKafkaConsumer(IConfluentConsumerWrapper consumerWrapper) => new(
        "test",
        consumerWrapper,
        new KafkaConsumerConfiguration(),
        Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
        Substitute.For<IBrokerClientCallbacksInvoker>(),
        Substitute.For<IKafkaOffsetStoreFactory>(),
        Substitute.For<IServiceProvider>(),
        Substitute.For<ISilverbackLogger<KafkaConsumer>>());

    private async Task<ConfluentConsumerWrapper> GetConnectedConsumerAsync(
        bool enableAutoCommit = true,
        bool commitOffsets = true,
        bool staticAssignment = false)
    {
        KafkaConsumerConfiguration configuration = new()
        {
            GroupId = "group",
            EnableAutoCommit = enableAutoCommit,
            CommitOffsetEach = enableAutoCommit ? null : 1,
            CommitOffsets = commitOffsets,
            Endpoints = new[]
            {
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new[]
                    {
                        new TopicPartitionOffset("topic", staticAssignment ? 0 : Partition.Any, Offset.Unset)
                    }.AsValueReadOnlyCollection()
                }
            }.AsValueReadOnlyCollection()
        };
        ConfluentConsumerWrapper consumer = new(
            "test",
            _consumerBuilder,
            configuration,
            _adminClientFactory,
            _callbacksInvoker,
            _offsetStoreFactory,
            _serviceProvider,
            _logger);
        consumer.Consumer = GetKafkaConsumer(consumer);

        await consumer.ConnectAsync();

        return consumer;
    }
}
