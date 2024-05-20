// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Consuming.ContextEnrichment;
using Silverback.Messaging.Transactions;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Transactions;

public class SilverbackContextTransactionExtensionsFixture
{
    [Fact]
    public void InitKafkaTransaction_ShouldInitAndReturnTransaction()
    {
        SilverbackContext context = new();

        IKafkaTransaction transaction = context.InitKafkaTransaction();

        transaction.ShouldNotBeNull();
        context.GetKafkaTransaction().Should().BeSameAs(transaction);
    }

    [Fact]
    public void InitKafkaTransaction_ShouldSetTransactionalIdSuffix()
    {
        SilverbackContext context = new();

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
        SilverbackContext context = new();
        context.SetConsumedPartition(new TopicPartition("topic1", 42), true);

        IKafkaTransaction transaction = context.InitKafkaTransaction(baseSuffix);

        transaction.ShouldNotBeNull();
        transaction.TransactionalIdSuffix.Should().Be($"{baseSuffix}|topic1[42]");
    }

    [Fact]
    public void InitKafkaTransaction_ShouldNotAppendPartitionToTransactionalId_WhenNotProcessingIndependently()
    {
        SilverbackContext context = new();
        context.SetConsumedPartition(new TopicPartition("topic1", 42), false);

        IKafkaTransaction transaction = context.InitKafkaTransaction();

        transaction.ShouldNotBeNull();
        transaction.TransactionalIdSuffix.Should().BeNull();
    }

    [Fact]
    public void AddKafkaTransaction_ShouldSetTransaction()
    {
        SilverbackContext context = new();
        KafkaTransaction transaction = new(context);
        context.AddKafkaTransaction(transaction);

        context.GetKafkaTransaction().Should().Be(transaction);
    }

    [Fact]
    public void AddKafkaTransaction_ShouldThrow_WhenTransactionIsAlreadySet()
    {
        SilverbackContext context = new();
        context.AddKafkaTransaction(new KafkaTransaction(context));

        Action act = () => context.AddKafkaTransaction(new KafkaTransaction(context));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddKafkaTransaction_ShouldNotThrow_WhenAddingSameTransactionAgain()
    {
        SilverbackContext context = new();
        KafkaTransaction transaction = new(context);
        context.AddKafkaTransaction(transaction);

        Action act = () => context.AddKafkaTransaction(transaction);

        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveKafkaTransaction_ShouldRemoveTransaction()
    {
        SilverbackContext context = new();
        context.AddKafkaTransaction(new KafkaTransaction(context));

        context.RemoveKafkaTransaction();

        Action act = () => context.GetKafkaTransaction();
        act.Should().Throw<InvalidOperationException>().WithMessage("*not found.");
    }
}
