// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
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

public class ConfluentConsumerWrapperTests
{
    private readonly IConfluentConsumerBuilder _consumerBuilder = Substitute.For<IConfluentConsumerBuilder>();

    private readonly IConsumer<byte[]?, byte[]?> _confluentConsumer = Substitute.For<IConsumer<byte[]?, byte[]?>>();

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    private readonly IConfluentAdminClientFactory _adminClientFactory = Substitute.For<IConfluentAdminClientFactory>();

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory = Substitute.For<IKafkaOffsetStoreFactory>();

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private readonly ISilverbackLogger _logger = Substitute.For<ISilverbackLogger>();

    public ConfluentConsumerWrapperTests()
    {
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

    [Theory]
    [InlineData(ErrorCode.UnknownMemberId)]
    [InlineData(ErrorCode.Local_TimedOut)]
    [InlineData(ErrorCode.GroupAuthorizationFailed)]
    public async Task Commit_ShouldNotifyCallbackAndRethrow_WhenKafkaExceptionIsThrown(ErrorCode errorCode)
    {
        await using ConfluentConsumerWrapper consumer = await GetConnectedConsumerAsync();
        KafkaException exception = new(new Error(errorCode));
        IKafkaOffsetCommittedCallback callback = Substitute.For<IKafkaOffsetCommittedCallback>();
        _confluentConsumer.Commit().Returns(_ => throw exception);

        _callbacksInvoker.When(invoker => invoker.Invoke(Arg.Any<Action<IKafkaOffsetCommittedCallback>>()))
            .Do(call => call.Arg<Action<IKafkaOffsetCommittedCallback>>()(callback));

        Action act = consumer.Commit;

        act.ShouldThrow<KafkaException>().ShouldBeSameAs(exception);
        callback.Received(1).OnOffsetsCommitted(
            Arg.Is<CommittedOffsets>(offsets => offsets.Error.Code == errorCode),
            consumer.Consumer);
    }

    [Fact]
    public async Task Commit_ShouldNotifyCallbackAndRethrow_WhenPartitionCommitFails()
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

        act.ShouldThrow<TopicPartitionOffsetException>().ShouldBeSameAs(exception);
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
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The assertion delegate is awaited before the consumer is disposed")]
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

        Func<Task> act = () => consumer.DisconnectAsync().AsTask();

        (await act.ShouldThrowAsync<KafkaException>()).ShouldBeSameAs(exception);
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

    private static KafkaConsumer GetKafkaConsumer(IConfluentConsumerWrapper consumerWrapper) => new(
        "test",
        consumerWrapper,
        new KafkaConsumerConfiguration(),
        Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
        Substitute.For<IBrokerClientCallbacksInvoker>(),
        Substitute.For<IKafkaOffsetStoreFactory>(),
        Substitute.For<IServiceProvider>(),
        Substitute.For<ISilverbackLogger<KafkaConsumer>>());

    private async Task<ConfluentConsumerWrapper> GetConnectedConsumerAsync(bool enableAutoCommit = true)
    {
        KafkaConsumerConfiguration configuration = new()
        {
            GroupId = "group",
            EnableAutoCommit = enableAutoCommit,
            CommitOffsetEach = enableAutoCommit ? null : 1,
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
