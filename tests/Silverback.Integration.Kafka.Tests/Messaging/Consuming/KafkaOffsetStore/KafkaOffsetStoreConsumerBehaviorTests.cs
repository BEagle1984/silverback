// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming.KafkaOffsetStore;

public class KafkaOffsetStoreConsumerBehaviorTests
{
    private static readonly KafkaOffset ConsumedOffset = new("topic1", 1, 42);

    private readonly InMemoryKafkaOffsetStoreSettings _offsetStoreSettings = new();

    private readonly IKafkaOffsetStore _offsetStore = Substitute.For<IKafkaOffsetStore>();

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory = Substitute.For<IKafkaOffsetStoreFactory>();

    private readonly IConsumerTransactionManager _transactionManager = Substitute.For<IConsumerTransactionManager>();

    private readonly IServiceProvider _serviceProvider;

    private readonly LoggerSubstitute<KafkaOffsetStoreConsumerBehavior> _loggerSubstitute;

    public KafkaOffsetStoreConsumerBehaviorTests()
    {
        _offsetStoreFactory.GetStore(_offsetStoreSettings, Arg.Any<IServiceProvider>()).Returns(_offsetStore);
        _transactionManager.Committing.Returns(new AsyncEvent<ConsumerPipelineContext>());
        _transactionManager.Skipping.Returns(new AsyncEvent<ConsumerPipelineContext>());

        _serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddSingleton(_offsetStoreFactory)
            .AddLoggerSubstitute(LogLevel.Trace)
            .AddSilverback()
            .WithConnectionToMessageBroker());

        _loggerSubstitute = (LoggerSubstitute<KafkaOffsetStoreConsumerBehavior>)_serviceProvider
            .GetRequiredService<ILogger<KafkaOffsetStoreConsumerBehavior>>();
    }

    [Fact]
    public void SortIndex_ShouldBeRightAfterTransactionHandler() =>
        new KafkaOffsetStoreConsumerBehavior().SortIndex.ShouldBe(BrokerBehaviorsSortIndexes.Consumer.TransactionHandler + 1);

    [Fact]
    public async Task HandleAsync_ShouldOnlyCallNext_WhenConsumerIsNotKafkaConsumer()
    {
        ConsumerPipelineContext context = CreateContext(Substitute.For<IConsumer>());
        bool nextCalled = false;

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;

                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
        await _transactionManager.Committing.InvokeAsync(context);
        await _transactionManager.Skipping.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(context.SilverbackContext.GetKafkaOffsetStoreScope);
        _offsetStoreFactory.DidNotReceive().GetStore(Arg.Any<KafkaOffsetStoreSettings>(), Arg.Any<IServiceProvider>());
    }

    [Fact]
    public async Task HandleAsync_ShouldOnlyCallNext_WhenClientSideOffsetStoreIsNotConfigured()
    {
        await using AssignedKafkaConsumer kafkaConsumer = new(new KafkaConsumerConfiguration { GroupId = "group1" }, ConsumedOffset.TopicPartition);
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = CreateContext(kafkaConsumer.Consumer);
        bool nextCalled = false;

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(
            context,
            (_, _) =>
            {
                nextCalled = true;

                return ValueTask.CompletedTask;
            },
            CancellationToken.None);
        await _transactionManager.Committing.InvokeAsync(context);
        await _transactionManager.Skipping.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(context.SilverbackContext.GetKafkaOffsetStoreScope);
        _offsetStoreFactory.DidNotReceive().GetStore(Arg.Any<KafkaOffsetStoreSettings>(), Arg.Any<IServiceProvider>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSetOffsetStoreScopeAndCallNext_WhenClientSideOffsetStoreIsConfigured()
    {
        await using AssignedKafkaConsumer kafkaConsumer = CreateConsumerWithOffsetStore();
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = CreateContext(kafkaConsumer.Consumer);
        KafkaOffsetStoreScope? scopeInNext = null;

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(
            context,
            (nextContext, _) =>
            {
                scopeInNext = nextContext.SilverbackContext.GetKafkaOffsetStoreScope();

                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        scopeInNext.ShouldNotBeNull();
        scopeInNext.SilverbackContext.ShouldBeSameAs(_serviceProvider.GetRequiredService<SilverbackContext>());
        _offsetStoreFactory.Received(1).GetStore(_offsetStoreSettings, _serviceProvider);
        await _offsetStore.DidNotReceive().StoreOffsetsAsync(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<KafkaOffset>>(),
            Arg.Any<ISilverbackContext?>());
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreOffsetsInSilverbackContext_WhenCommitting()
    {
        await using AssignedKafkaConsumer kafkaConsumer = CreateConsumerWithOffsetStore();
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = CreateContext(kafkaConsumer.Consumer);

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None);
        await _transactionManager.Committing.InvokeAsync(context);

        await _offsetStore.Received(1).StoreOffsetsAsync(
            "group1",
            Arg.Is<IEnumerable<KafkaOffset>>(offsets => offsets.SequenceEqual(new[] { ConsumedOffset })),
            _serviceProvider.GetRequiredService<SilverbackContext>());
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreOffsetsOutsideTransaction_WhenSkipping()
    {
        await using AssignedKafkaConsumer kafkaConsumer = CreateConsumerWithOffsetStore();
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = CreateContext(kafkaConsumer.Consumer);

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None);
        await _transactionManager.Skipping.InvokeAsync(context);

        await _offsetStore.Received(1).StoreOffsetsAsync(
            "group1",
            Arg.Is<IEnumerable<KafkaOffset>>(offsets => offsets.SequenceEqual(new[] { ConsumedOffset })),
            Arg.Is<ISilverbackContext?>(storeContext => storeContext == null));
        _loggerSubstitute.DidNotReceive(LogLevel.Error, typeof(InvalidOperationException));
    }

    [Fact]
    public async Task HandleAsync_ShouldRetryStoringSkippedOffsets_WhenStoreFailsOnce()
    {
        _offsetStore.StoreOffsetsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<KafkaOffset>>(), Arg.Any<ISilverbackContext?>())
            .Returns(Task.FromException(new InvalidOperationException("Store failed.")), Task.CompletedTask);
        await using AssignedKafkaConsumer kafkaConsumer = CreateConsumerWithOffsetStore();
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = CreateContext(kafkaConsumer.Consumer);

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None);
        await _transactionManager.Skipping.InvokeAsync(context);

        await _offsetStore.Received(2).StoreOffsetsAsync(
            "group1",
            Arg.Is<IEnumerable<KafkaOffset>>(offsets => offsets.SequenceEqual(new[] { ConsumedOffset })),
            Arg.Is<ISilverbackContext?>(storeContext => storeContext == null));
        _loggerSubstitute.DidNotReceive(LogLevel.Error, typeof(InvalidOperationException));
    }

    [Fact]
    public async Task HandleAsync_ShouldLogErrorAndNotThrow_WhenStoringSkippedOffsetsFailsTwice()
    {
        _offsetStore.StoreOffsetsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<KafkaOffset>>(), Arg.Any<ISilverbackContext?>())
            .ThrowsAsync(new InvalidOperationException("Store failed."));
        await using AssignedKafkaConsumer kafkaConsumer = CreateConsumerWithOffsetStore();
        await kafkaConsumer.StartAsync();
        ConsumerPipelineContext context = CreateContext(kafkaConsumer.Consumer);

        await new KafkaOffsetStoreConsumerBehavior().HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None);
        await _transactionManager.Skipping.InvokeAsync(context);

        await _offsetStore.Received(2).StoreOffsetsAsync(
            "group1",
            Arg.Is<IEnumerable<KafkaOffset>>(offsets => offsets.SequenceEqual(new[] { ConsumedOffset })),
            Arg.Is<ISilverbackContext?>(storeContext => storeContext == null));
        const string expectedMessage =
            "Error occurred storing the offsets of the skipped messages to the client side offset store; the messages might be consumed " +
            "again after a restart or rebalance | Offsets: topic1[1]@42, ConsumerName: consumer1";
        _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidOperationException), expectedMessage, 2046, "Store failed.");
    }

    private AssignedKafkaConsumer CreateConsumerWithOffsetStore() =>
        new(
            new KafkaConsumerConfiguration
            {
                GroupId = "group1",
                ClientSideOffsetStore = _offsetStoreSettings
            },
            ConsumedOffset.TopicPartition);

    private ConsumerPipelineContext CreateContext(IConsumer consumer) =>
        ConsumerPipelineContextHelper.CreateSubstitute(
            serviceProvider: _serviceProvider,
            transactionManager: _transactionManager,
            consumer: consumer,
            identifier: ConsumedOffset);
}
