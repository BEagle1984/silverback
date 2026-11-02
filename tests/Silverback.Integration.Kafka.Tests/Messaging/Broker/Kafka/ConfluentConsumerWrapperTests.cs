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

        nativeCalls.ShouldBe(["StoreOffset", "Close", "Dispose"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The late call completes before the consumer is disposed")]
    public async Task DisconnectAsync_ShouldNotDeadlock_WhenCloseWaitsForStoreOffsetOrCommitFromAnotherThread(bool commit)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        TopicPartitionOffset offset = new("topic", 0, 2);
        Action act = commit ? consumer.Commit : () => consumer.StoreOffset(offset);
        Task lateCall = Task.CompletedTask;
        bool lateCallCompleted = false;

        // Like the revocation callbacks waiting for the processing, which stores and commits the offsets
        _confluentConsumer.When(native => native.Close()).Do(_ =>
        {
            lateCall = Task.Run(act);
            lateCallCompleted = SpinWait.SpinUntil(() => lateCall.IsCompleted, Timeout);
        });

        await consumer.DisconnectAsync().AsTask().WaitAsync(Timeout);

        lateCallCompleted.ShouldBeTrue();
        await Should.ThrowAsync<InvalidOperationException>(lateCall);
        _confluentConsumer.DidNotReceive().StoreOffset(Arg.Any<TopicPartitionOffset>());
        _confluentConsumer.DidNotReceive().Commit();
        _confluentConsumer.Received(1).Close();
        _confluentConsumer.Received(1).Dispose();
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

    private async Task<ConfluentConsumerWrapper> GetConnectedConsumerAsync(bool enableAutoCommit = true, bool commitOffsets = true)
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
                        new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)
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
