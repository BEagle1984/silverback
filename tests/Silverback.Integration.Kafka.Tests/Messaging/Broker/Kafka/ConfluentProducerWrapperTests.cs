// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConfluentProducerWrapperTests
{
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

        producer.InitTransactions();
        producer.BeginTransaction();

        _confluentProducer1.Received(1).Dispose();
        _confluentProducer2.Received(1).InitTransactions(Arg.Any<TimeSpan>());
        _confluentProducer2.Received(1).BeginTransaction();
    }

    private static KafkaException CreateFatalException() => new(new Error(ErrorCode.Local_Fatal, "Fatal error", true));

    private async Task<ConfluentProducerWrapper> CreateConnectedProducerAsync()
    {
        ConfluentProducerWrapper producer = new("test", _producerBuilder, new KafkaProducerConfiguration(), _callbacksInvoker, _logger);
        await producer.ConnectAsync();

        return producer;
    }
}
