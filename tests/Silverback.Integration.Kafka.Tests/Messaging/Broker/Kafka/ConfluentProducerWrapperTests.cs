// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentProducerWrapperTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static readonly TopicPartition TopicPartition = new("topic", Partition.Any);

    private readonly IConfluentProducerBuilder _producerBuilder = Substitute.For<IConfluentProducerBuilder>();

    private readonly IProducer<byte[]?, byte[]?> _confluentProducer1 = Substitute.For<IProducer<byte[]?, byte[]?>>();

    private readonly IProducer<byte[]?, byte[]?> _confluentProducer2 = Substitute.For<IProducer<byte[]?, byte[]?>>();

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker = Substitute.For<IBrokerClientCallbacksInvoker>();

    private readonly SilverbackLoggerSubstitute<ConfluentProducerWrapper> _logger = new();

    public ConfluentProducerWrapperTests()
    {
        _producerBuilder.SetConfiguration(Arg.Any<ProducerConfig>()).Returns(_producerBuilder);
        _producerBuilder.SetStatisticsHandler(Arg.Any<Action<IProducer<byte[]?, byte[]?>, string>>()).Returns(_producerBuilder);
        _producerBuilder.SetErrorHandler(Arg.Any<Action<IProducer<byte[]?, byte[]?>, Error>>()).Returns(_producerBuilder);
        _producerBuilder.SetLogHandler(Arg.Any<Action<IProducer<byte[]?, byte[]?>, LogMessage>>()).Returns(_producerBuilder);
        _producerBuilder.Build().Returns(_confluentProducer1, _confluentProducer2);
    }

    [Fact]
    public async Task ProduceAsync_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Func<Task> act = () => producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        await act.ShouldThrowAsync<KafkaException>();
        _confluentProducer1.Received(1).Dispose();

        await producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        _producerBuilder.Received(2).Build();
        await _confluentProducer2.Received(1).ProduceAsync(
            TopicPartition,
            Arg.Any<Message<byte[]?, byte[]?>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProduceAsync_ShouldNotDisposeProducer_WhenNonFatalErrorOccurs()
    {
        _confluentProducer1
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<DeliveryResult<byte[]?, byte[]?>>(new KafkaException(ErrorCode.MsgSizeTooLarge)),
                Task.FromResult(new DeliveryResult<byte[]?, byte[]?>()));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Func<Task> act = () => producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        await act.ShouldThrowAsync<KafkaException>();
        await producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        _confluentProducer1.DidNotReceive().Dispose();
        _producerBuilder.Received(1).Build();
        await _confluentProducer1.Received(2).ProduceAsync(
            TopicPartition,
            Arg.Any<Message<byte[]?, byte[]?>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProduceAsync_ShouldNotDisposeRecreatedProducer_WhenPendingProduceOfDisposedProducerFails()
    {
        TaskCompletionSource<DeliveryResult<byte[]?, byte[]?>> pendingProduceTaskCompletionSource = new();
        _confluentProducer1
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .Returns(
                pendingProduceTaskCompletionSource.Task,
                Task.FromException<DeliveryResult<byte[]?, byte[]?>>(CreateFatalException()));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Task<DeliveryResult<byte[]?, byte[]?>> pendingProduceTask = producer.ProduceAsync(
            TopicPartition,
            new Message<byte[]?, byte[]?>(),
            CancellationToken.None);
        Func<Task> act = () => producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        await act.ShouldThrowAsync<KafkaException>();
        await producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        pendingProduceTaskCompletionSource.SetException(CreateFatalException());

        await Should.ThrowAsync<KafkaException>(pendingProduceTask);
        await producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None);

        _confluentProducer1.Received(1).Dispose();
        _confluentProducer2.DidNotReceive().Dispose();
        _producerBuilder.Received(2).Build();
        await _confluentProducer2.Received(2).ProduceAsync(
            TopicPartition,
            Arg.Any<Message<byte[]?, byte[]?>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Produce_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.Produce(
                Arg.Any<TopicPartition>(),
                Arg.Any<Message<byte[]?, byte[]?>>(),
                Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>()))
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = () => producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { });

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.Received(1).Dispose();

        producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { });

        _producerBuilder.Received(2).Build();
        _confluentProducer2.Received(1).Produce(
            TopicPartition,
            Arg.Any<Message<byte[]?, byte[]?>>(),
            Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>());
    }

    [Fact]
    public async Task Produce_ShouldNotDisposeProducer_WhenNonFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.Produce(
                Arg.Any<TopicPartition>(),
                Arg.Any<Message<byte[]?, byte[]?>>(),
                Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>()))
            .Throw(new KafkaException(ErrorCode.Local_QueueFull));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = () => producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { });

        act.ShouldThrow<KafkaException>();

        _confluentProducer1.DidNotReceive().Dispose();
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task BeginTransaction_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.BeginTransaction())
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        producer.InitTransactions();
        Action act = producer.BeginTransaction;

        act.ShouldThrow<KafkaException>();

        producer.BeginTransaction();

        _confluentProducer1.Received(1).Dispose();
        _confluentProducer2.Received(1).InitTransactions(Arg.Any<TimeSpan>());
        _confluentProducer2.Received(1).BeginTransaction();
    }

    [Fact]
    public async Task BeginTransaction_ShouldInitTransactions_WhenReconnected()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();
        producer.InitTransactions();
        producer.BeginTransaction();

        await producer.DisconnectAsync();
        await producer.ConnectAsync();
        producer.BeginTransaction();

        _confluentProducer1.Received(1).InitTransactions(Arg.Any<TimeSpan>());
        _confluentProducer2.Received(1).InitTransactions(Arg.Any<TimeSpan>());
        _confluentProducer2.Received(1).BeginTransaction();
    }

    [Fact]
    public async Task InitTransactions_ShouldInitTransactionsOnce()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        producer.InitTransactions();
        producer.InitTransactions();
        producer.BeginTransaction();

        _confluentProducer1.Received(1).InitTransactions(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task InitTransactions_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.InitTransactions(Arg.Any<TimeSpan>()))
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = producer.InitTransactions;

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.Received(1).Dispose();
        _logger.DidNotReceive(LogLevel.Trace, null, eventId: 2070);

        producer.InitTransactions();

        _confluentProducer2.Received(1).InitTransactions(TimeSpan.FromSeconds(30));
        _logger.Received(LogLevel.Trace, null, eventId: 2070);
    }

    [Fact]
    public async Task InitTransactions_ShouldNotDisposeProducer_WhenNonFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.InitTransactions(Arg.Any<TimeSpan>()))
            .Throw(new KafkaException(ErrorCode.Local_TimedOut));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = producer.InitTransactions;

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.DidNotReceive().Dispose();
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task CommitTransaction_ShouldCommitWithConfiguredTimeoutAndLog()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync(
            new KafkaProducerConfiguration
            {
                TransactionalId = "transactional-id",
                TransactionCommitTimeout = TimeSpan.FromSeconds(42)
            });

        producer.CommitTransaction();

        _confluentProducer1.Received(1).CommitTransaction(TimeSpan.FromSeconds(42));
        _logger.Received(
            LogLevel.Information,
            null,
            "Transaction committed | ProducerName: test, TransactionalId: transactional-id",
            2072);
    }

    [Fact]
    public async Task CommitTransaction_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.CommitTransaction(Arg.Any<TimeSpan>()))
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = producer.CommitTransaction;

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.Received(1).Dispose();
        _logger.DidNotReceive(LogLevel.Information, null, eventId: 2072);

        producer.CommitTransaction();

        _producerBuilder.Received(2).Build();
        _confluentProducer2.Received(1).CommitTransaction(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task CommitTransaction_ShouldNotDisposeProducer_WhenNonFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.CommitTransaction(Arg.Any<TimeSpan>()))
            .Throw(new KafkaException(ErrorCode.Local_TimedOut));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = producer.CommitTransaction;

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.DidNotReceive().Dispose();
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task AbortTransaction_ShouldAbortWithConfiguredTimeoutAndLog()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync(
            new KafkaProducerConfiguration
            {
                TransactionalId = "transactional-id",
                TransactionAbortTimeout = TimeSpan.FromSeconds(42)
            });

        producer.AbortTransaction();

        _confluentProducer1.Received(1).AbortTransaction(TimeSpan.FromSeconds(42));
        _logger.Received(
            LogLevel.Information,
            null,
            "Transaction aborted | ProducerName: test, TransactionalId: transactional-id",
            2073);
    }

    [Fact]
    public async Task AbortTransaction_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.AbortTransaction(Arg.Any<TimeSpan>()))
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = producer.AbortTransaction;

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.Received(1).Dispose();
        _logger.DidNotReceive(LogLevel.Information, null, eventId: 2073);

        producer.AbortTransaction();

        _producerBuilder.Received(2).Build();
        _confluentProducer2.Received(1).AbortTransaction(Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task AbortTransaction_ShouldNotDisposeProducer_WhenNonFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.AbortTransaction(Arg.Any<TimeSpan>()))
            .Throw(new KafkaException(ErrorCode.Local_TimedOut));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = producer.AbortTransaction;

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.DidNotReceive().Dispose();
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task SendOffsetsToTransaction_ShouldSendOffsetsWithConfiguredTimeoutAndLogEachOffset()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync(
            new KafkaProducerConfiguration
            {
                TransactionalId = "transactional-id",
                SendOffsetToTransactionTimeout = TimeSpan.FromSeconds(42)
            });
        TopicPartitionOffset[] offsets = [new("topic1", 0, 1), new("topic2", 3, 4)];
        IConsumerGroupMetadata groupMetadata = Substitute.For<IConsumerGroupMetadata>();

        producer.SendOffsetsToTransaction(offsets, groupMetadata);

        _confluentProducer1.Received(1).SendOffsetsToTransaction(offsets, groupMetadata, TimeSpan.FromSeconds(42));
        _logger.Received(
            LogLevel.Debug,
            null,
            "Offset topic1[0]@1 sent to transaction | ProducerName: test, TransactionalId: transactional-id",
            2074);
        _logger.Received(
            LogLevel.Debug,
            null,
            "Offset topic2[3]@4 sent to transaction | ProducerName: test, TransactionalId: transactional-id",
            2074);
    }

    [Fact]
    public async Task SendOffsetsToTransaction_ShouldRecreateProducer_WhenFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.SendOffsetsToTransaction(
                Arg.Any<IEnumerable<TopicPartitionOffset>>(),
                Arg.Any<IConsumerGroupMetadata>(),
                Arg.Any<TimeSpan>()))
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();
        TopicPartitionOffset[] offsets = [new("topic1", 0, 1)];
        IConsumerGroupMetadata groupMetadata = Substitute.For<IConsumerGroupMetadata>();

        Action act = () => producer.SendOffsetsToTransaction(offsets, groupMetadata);

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.Received(1).Dispose();
        _logger.DidNotReceive(LogLevel.Debug, null, eventId: 2074);

        producer.SendOffsetsToTransaction(offsets, groupMetadata);

        _producerBuilder.Received(2).Build();
        _confluentProducer2.Received(1).SendOffsetsToTransaction(offsets, groupMetadata, Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task SendOffsetsToTransaction_ShouldNotDisposeProducer_WhenNonFatalErrorOccurs()
    {
        _confluentProducer1
            .When(confluentProducer => confluentProducer.SendOffsetsToTransaction(
                Arg.Any<IEnumerable<TopicPartitionOffset>>(),
                Arg.Any<IConsumerGroupMetadata>(),
                Arg.Any<TimeSpan>()))
            .Throw(new KafkaException(ErrorCode.Local_TimedOut));
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        Action act = () => producer.SendOffsetsToTransaction([new TopicPartitionOffset("topic1", 0, 1)], Substitute.For<IConsumerGroupMetadata>());

        act.ShouldThrow<KafkaException>();
        _confluentProducer1.DidNotReceive().Dispose();
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task Produce_ShouldThrow_WhenNotConnected()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();
        await producer.DisconnectAsync();

        Action act = () => producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { });

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The producer is not connected.");
        _confluentProducer1.DidNotReceive().Produce(
            Arg.Any<TopicPartition>(),
            Arg.Any<Message<byte[]?, byte[]?>>(),
            Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>());
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task Produce_ShouldThrowWithoutRecreatingProducer_WhenDisconnectingWhileWaitingForRecreatedProducer()
    {
        TaskCompletionSource buildStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseBuild = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _producerBuilder.Build().Returns(
            _ => _confluentProducer1,
            _ =>
            {
                buildStarted.TrySetResult();
                releaseBuild.Task.Wait(Timeout);
                return _confluentProducer2;
            });
        _confluentProducer1
            .When(confluentProducer => confluentProducer.Produce(
                Arg.Any<TopicPartition>(),
                Arg.Any<Message<byte[]?, byte[]?>>(),
                Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>()))
            .Throw(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();
        Should.Throw<KafkaException>(() => producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { }));

        // A first call recreates the producer disposed after the fatal error, holding the lock
        Task recreatingProduceTask = Task.Run(() => producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { }));
        await buildStarted.Task.WaitAsync(Timeout);

        // A second call, still seeing the producer connected, waits for the lock
        Exception? waitingProduceException = null;
        Thread waitingProduceThread = new(() => waitingProduceException = Record.Exception(
            () => producer.Produce(TopicPartition, new Message<byte[]?, byte[]?>(), _ => { })));
        waitingProduceThread.Start();
        SpinWait.SpinUntil(() => (waitingProduceThread.ThreadState & ThreadState.WaitSleepJoin) != 0, Timeout).ShouldBeTrue();

        // The disconnect starts before the lock is released
        Task disconnectTask = Task.Run(() => producer.DisconnectAsync().AsTask());
        await AsyncTestingUtil.WaitAsync(() => producer.Status == ClientStatus.Disconnecting, Timeout);
        producer.Status.ShouldBe(ClientStatus.Disconnecting);

        releaseBuild.SetResult();
        waitingProduceThread.Join(Timeout).ShouldBeTrue();
        await Task.WhenAll(recreatingProduceTask, disconnectTask).WaitAsync(Timeout);

        waitingProduceException.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("The producer is not connected.");
        _producerBuilder.Received(2).Build();
        _confluentProducer2.Received(1).Produce(
            Arg.Any<TopicPartition>(),
            Arg.Any<Message<byte[]?, byte[]?>>(),
            Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>());
    }

    [Fact]
    public async Task DisconnectAsync_ShouldNotFlushDisposedProducer_WhenDisposedAfterFatalError()
    {
        _confluentProducer1
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateFatalException());
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();
        await Should.ThrowAsync<KafkaException>(() => producer.ProduceAsync(TopicPartition, new Message<byte[]?, byte[]?>(), CancellationToken.None));

        await producer.DisconnectAsync();

        producer.Status.ShouldBe(ClientStatus.Disconnected);
        _confluentProducer1.DidNotReceive().Flush(Arg.Any<CancellationToken>());
        _confluentProducer1.Received(1).Dispose();
        _producerBuilder.Received(1).Build();
    }

    [Fact]
    public async Task DisconnectAsync_ShouldFlushAndDisposeProducer()
    {
        ConfluentProducerWrapper producer = await CreateConnectedProducerAsync();

        await producer.DisconnectAsync();

        producer.Status.ShouldBe(ClientStatus.Disconnected);
        _confluentProducer1.Received(1).Flush(Arg.Any<CancellationToken>());
        _confluentProducer1.Received(1).Dispose();
    }

    [Fact]
    public void Producer_ShouldThrow_WhenNotInitialized()
    {
        ConfluentProducerWrapper producer = new("test", _producerBuilder, new KafkaProducerConfiguration(), _callbacksInvoker, _logger);

        Action act = () => _ = producer.Producer;

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("The producer is not initialized yet.");
    }

    [Fact]
    public void Producer_ShouldReturnKafkaProducerUsingWrapper()
    {
        ConfluentProducerWrapper producer = new("test", _producerBuilder, new KafkaProducerConfiguration(), _callbacksInvoker, _logger);

        KafkaProducer kafkaProducer = new(
            "test",
            producer,
            new KafkaProducerConfiguration
            {
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic")
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());

        producer.Producer.ShouldBeSameAs(kafkaProducer);
    }

    private static KafkaException CreateFatalException() => new(new Error(ErrorCode.Local_Fatal, "Fatal error", true));

    private async Task<ConfluentProducerWrapper> CreateConnectedProducerAsync(KafkaProducerConfiguration? configuration = null)
    {
        ConfluentProducerWrapper producer = new("test", _producerBuilder, configuration ?? new KafkaProducerConfiguration(), _callbacksInvoker, _logger);
        await producer.ConnectAsync();

        return producer;
    }
}
