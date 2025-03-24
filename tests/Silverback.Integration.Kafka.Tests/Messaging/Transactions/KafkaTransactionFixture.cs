// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Transactions;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Transactions;

public class KafkaTransactionFixture
{
    [Fact]
    public void Commit_ShouldCommitTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.Commit();

        confluentProducer.Received(1).CommitTransaction();
    }

    [Fact]
    public void Commit_ShouldSendConsumedMessageOffsetsToTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        KafkaConsumer consumer = new(
            "consumer1",
            confluentConsumerWrapper,
            new KafkaConsumerConfiguration
            {
                SendOffsetsToTransaction = true
            },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IConsumerLogger<KafkaConsumer>>());
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                identifier: new KafkaOffset("test", 1, 42),
                consumer: consumer));

        transaction.Commit();

        confluentProducer.Received(1).SendOffsetsToTransaction(
            Arg.Is<TopicPartitionOffset[]>(
                offsets =>
                    offsets.Length == 1 &&
                    offsets[0].Topic == "test" &&
                    offsets[0].Partition.Value == 1 &&
                    offsets[0].Offset == 43),
            Arg.Any<IConsumerGroupMetadata>());
    }

    [Fact]
    public void Commit_ShouldSendSequenceOffsetsToTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        KafkaConsumer consumer = new(
            "consumer1",
            confluentConsumerWrapper,
            new KafkaConsumerConfiguration
            {
                SendOffsetsToTransaction = true
            },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IConsumerLogger<KafkaConsumer>>());
        ISequence sequence = Substitute.For<ISequence>();
        sequence.GetCommitIdentifiers().Returns(
        [
            new KafkaOffset("test", 2, 4),
            new KafkaOffset("test", 4, 2)
        ]);
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                identifier: new KafkaOffset("test", 1, 42),
                sequence: sequence,
                consumer: consumer));

        transaction.Commit();

        confluentProducer.Received(1).SendOffsetsToTransaction(
            Arg.Is<TopicPartitionOffset[]>(
                offsets =>
                    offsets.Length == 2 &&
                    offsets[0].Topic == "test" &&
                    offsets[0].Partition.Value == 2 &&
                    offsets[0].Offset == 5 &&
                    offsets[1].Topic == "test" &&
                    offsets[1].Partition.Value == 4 &&
                    offsets[1].Offset == 3),
            Arg.Any<IConsumerGroupMetadata>());
    }

    [Fact]
    public void Commit_ShouldNotSendOffsets_WhenSendOffSetToTransactionIsFalse()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        KafkaConsumer consumer = new(
            "consumer1",
            confluentConsumerWrapper,
            new KafkaConsumerConfiguration
            {
                SendOffsetsToTransaction = false
            },
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IConsumerLogger<KafkaConsumer>>());
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                identifier: new KafkaOffset("test", 1, 42),
                consumer: consumer));

        transaction.Commit();

        confluentProducer.DidNotReceive().SendOffsetsToTransaction(
            Arg.Any<IReadOnlyCollection<TopicPartitionOffset>>(),
            Arg.Any<IConsumerGroupMetadata>());
    }

    [Fact]
    public void Abort_ShouldAbortTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.Abort();

        confluentProducer.Received(1).AbortTransaction();
    }

    [Fact]
    public void Dispose_ShouldAbortTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.Dispose();

        confluentProducer.Received(1).AbortTransaction();
    }

    [Fact]
    public void Dispose_ShouldNotAbortTransaction_WhenNotPending()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        transaction.Dispose();

        confluentProducer.DidNotReceive().AbortTransaction();
    }

    [Fact]
    public void Begin_ShouldBeginTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        transaction.Begin();

        confluentProducer.Received(1).BeginTransaction();
    }

    [Fact]
    public void EnsureBegin_ShouldBeginTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        transaction.EnsureBegin();

        confluentProducer.Received(1).BeginTransaction();
    }

    [Fact]
    public void EnsureBegin_ShouldNotBeginTransaction_WhenAlreadyPending()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.EnsureBegin();

        confluentProducer.Received(1).BeginTransaction();
    }
}
