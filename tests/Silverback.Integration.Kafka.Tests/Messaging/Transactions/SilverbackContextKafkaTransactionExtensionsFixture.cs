﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Transactions;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Transactions;

public class SilverbackContextKafkaTransactionExtensionsFixture
{
    [Fact]
    public void InitKafkaTransaction_ShouldInitAndReturnTransaction()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        IKafkaTransaction transaction = context.InitKafkaTransaction();

        transaction.ShouldNotBeNull();
        context.GetKafkaTransaction().Should().BeSameAs(transaction);
    }

    [Fact]
    public void InitKafkaTransaction_ShouldSetTransactionalIdSuffix()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        IKafkaTransaction transaction = context.InitKafkaTransaction("suffix");

        transaction.ShouldNotBeNull();
        transaction.TransactionalIdSuffix.Should().Be("suffix");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("suffix")]
    public void InitKafkaTransaction_ShouldAppendPartitionToTransactionalId_WhenInConsumerContext(string? baseSuffix)
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        KafkaConsumer kafkaConsumer = new(
            "consumer1",
            confluentConsumerWrapper,
            new KafkaConsumerConfigurationBuilder(Substitute.For<IServiceProvider>())
                .WithBootstrapServers("PLAINTEXT://tests:9092")
                .WithGroupId("group1")
                .ProcessPartitionsIndependently()
                .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                .Build(),
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IConsumerLogger<KafkaConsumer>>());
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                consumer: kafkaConsumer,
                identifier: new KafkaOffset("topic1", 42, 1)));

        IKafkaTransaction transaction = context.InitKafkaTransaction(baseSuffix);

        transaction.ShouldNotBeNull();
        transaction.TransactionalIdSuffix.Should().Be($"{baseSuffix}|topic1[42]");
    }

    [Fact]
    public void InitKafkaTransaction_ShouldNotAppendPartitionToTransactionalId_WhenNotProcessingIndependently()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        KafkaConsumer kafkaConsumer = new(
            "consumer1",
            confluentConsumerWrapper,
            new KafkaConsumerConfigurationBuilder(Substitute.For<IServiceProvider>())
                .WithBootstrapServers("PLAINTEXT://tests:9092")
                .WithGroupId("group1")
                .ProcessAllPartitionsTogether()
                .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                .Build(),
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IConsumerLogger<KafkaConsumer>>());
        context.SetConsumerPipelineContext(
            ConsumerPipelineContextHelper.CreateSubstitute(
                consumer: kafkaConsumer,
                identifier: new KafkaOffset("topic1", 42, 1)));

        IKafkaTransaction transaction = context.InitKafkaTransaction();

        transaction.ShouldNotBeNull();
        transaction.TransactionalIdSuffix.Should().BeNull();
    }

    [Fact]
    public void AddKafkaTransaction_ShouldSetTransaction()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        context.AddKafkaTransaction(transaction);

        context.GetKafkaTransaction().Should().Be(transaction);
    }

    [Fact]
    public void AddKafkaTransaction_ShouldThrow_WhenTransactionIsAlreadySet()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.AddKafkaTransaction(new KafkaTransaction(context));

        Action act = () => context.AddKafkaTransaction(new KafkaTransaction(context));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddKafkaTransaction_ShouldNotThrow_WhenAddingSameTransactionAgain()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        KafkaTransaction transaction = new(context);
        context.AddKafkaTransaction(transaction);

        Action act = () => context.AddKafkaTransaction(transaction);

        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveKafkaTransaction_ShouldRemoveTransaction()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.AddKafkaTransaction(new KafkaTransaction(context));

        context.RemoveKafkaTransaction();

        Action act = () => context.GetKafkaTransaction();
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found.");
    }
}