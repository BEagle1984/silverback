// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
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

public class KafkaTransactionTests
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
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                identifier: new KafkaOffset("test", 1, 42),
                consumer: consumer));

        transaction.Commit();

        confluentProducer.Received(1).SendOffsetsToTransaction(
            Arg.Is<IReadOnlyList<TopicPartitionOffset>>(offsets =>
                offsets.Count == 1 &&
                offsets[0].Topic == "test" &&
                offsets[0].Partition.Value == 1 &&
                offsets[0].Offset.Value == 43),
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
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
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
            Arg.Is<IReadOnlyCollection<TopicPartitionOffset>>(offsets =>
                offsets.Count == 2 &&
                offsets.ElementAt(0).Topic == "test" &&
                offsets.ElementAt(0).Partition.Value == 2 &&
                offsets.ElementAt(0).Offset == 5 &&
                offsets.ElementAt(1).Topic == "test" &&
                offsets.ElementAt(1).Partition.Value == 4 &&
                offsets.ElementAt(1).Offset == 3),
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
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
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

    [Fact]
    public void EnsureBegin_ShouldThrow_WhenNotBoundToProducer()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);

        Action act = transaction.EnsureBegin;

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not bound to a producer.");
    }

    [Fact]
    public void Begin_ShouldThrow_WhenNotBoundToProducer()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);

        Action act = transaction.Begin;

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not bound to a producer.");
    }

    [Fact]
    public void Commit_ShouldThrow_WhenNotBoundToProducer()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);

        Action act = transaction.Commit;

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not bound to a producer.");
    }

    [Fact]
    public void Commit_ShouldThrow_WhenNotPending()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        Action act = transaction.Commit;

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not pending.");
        confluentProducer.DidNotReceive().CommitTransaction();
    }

    [Fact]
    public void Commit_ShouldNotSendOffsets_WhenNotInConsumerPipelineContext()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.Commit();

        confluentProducer.DidNotReceive().SendOffsetsToTransaction(
            Arg.Any<IReadOnlyCollection<TopicPartitionOffset>>(),
            Arg.Any<IConsumerGroupMetadata>());
        confluentProducer.Received(1).CommitTransaction();
    }

    [Fact]
    public void Commit_ShouldNotSendOffsets_WhenConsumerIsNotKafkaConsumer()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                identifier: new KafkaOffset("test", 1, 42),
                consumer: Substitute.For<IConsumer>()));

        transaction.Commit();

        confluentProducer.DidNotReceive().SendOffsetsToTransaction(
            Arg.Any<IReadOnlyCollection<TopicPartitionOffset>>(),
            Arg.Any<IConsumerGroupMetadata>());
        confluentProducer.Received(1).CommitTransaction();
    }

    [Fact]
    public void Commit_ShouldUnbindProducerAndCompleteTransaction()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        IConfluentProducerWrapper otherConfluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.Commit();

        Action abort = transaction.Abort;
        abort.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not pending.");
        Action ensureBegin = transaction.EnsureBegin;
        ensureBegin.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not bound to a producer.");

        transaction.BindConfluentProducer(otherConfluentProducer);
        transaction.EnsureBegin();

        otherConfluentProducer.Received(1).BeginTransaction();
    }

    [Fact]
    public void Abort_ShouldThrow_WhenNotPending()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        Action act = transaction.Abort;

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is not pending.");
        confluentProducer.DidNotReceive().AbortTransaction();
    }

    [Fact]
    public void Abort_ShouldAbortTransaction_WhenCommitFailed()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        confluentProducer.When(producer => producer.CommitTransaction()).Throw(new InvalidOperationException("test"));
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();
        Action commit = transaction.Commit;
        commit.ShouldThrow<InvalidOperationException>();

        transaction.Abort();

        confluentProducer.Received(1).AbortTransaction();
    }

    [Fact]
    public void AbortIfPending_ShouldAbortTransaction_WhenPending()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);
        transaction.Begin();

        transaction.AbortIfPending();

        confluentProducer.Received(1).AbortTransaction();
    }

    [Fact]
    public void AbortIfPending_ShouldNotAbortTransaction_WhenNotPending()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        transaction.AbortIfPending();

        confluentProducer.DidNotReceive().AbortTransaction();
    }

    [Fact]
    public void Dispose_ShouldRemoveTransactionFromContext()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);

        transaction.Dispose();

        Action act = () => context.GetKafkaTransaction();
        act.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void BindConfluentProducer_ShouldNotThrow_WhenBindingSameProducerAgain()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        transaction.BindConfluentProducer(confluentProducer);

        transaction.Begin();
        confluentProducer.Received(1).BeginTransaction();
    }

    [Fact]
    public void BindConfluentProducer_ShouldThrow_WhenAlreadyBoundToAnotherProducer()
    {
        IConfluentProducerWrapper confluentProducer = Substitute.For<IConfluentProducerWrapper>();
        IConfluentProducerWrapper otherConfluentProducer = Substitute.For<IConfluentProducerWrapper>();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        transaction.BindConfluentProducer(confluentProducer);

        Action act = () => transaction.BindConfluentProducer(otherConfluentProducer);

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The transaction is already bound to a producer.");
    }
}
