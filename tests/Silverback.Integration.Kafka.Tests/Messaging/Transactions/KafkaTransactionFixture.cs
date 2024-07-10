// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Silverback.Messaging.Broker.Kafka;
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
