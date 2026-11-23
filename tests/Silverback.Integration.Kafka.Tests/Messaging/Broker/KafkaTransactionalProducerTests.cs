// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Transactions;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public sealed class KafkaTransactionalProducerTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly KafkaProducerConfiguration _configuration;

    private readonly ConcurrentQueue<Message<byte[]?, byte[]?>> _produced = new();

    private readonly IConfluentProducerWrapper _client;

    private readonly KafkaProducer _innerProducer;

    private readonly IKafkaTransactionalProducerCollection _transactionalProducers;

    private readonly KafkaTransactionalProducer _producer;

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public KafkaTransactionalProducerTests()
    {
        _configuration = new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .EnableTransactions("transactional1")
            .Produce<object>(endpoint => endpoint.ProduceTo("topic1"))
            .Build();

        _client = CreateClient(_configuration, _produced);

        IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
        behaviors.GetBehaviorsList().Returns([]);
        _innerProducer = new KafkaProducer(
            "producer1|suffix",
            _client,
            _configuration,
            behaviors,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());

        _transactionalProducers = Substitute.For<IKafkaTransactionalProducerCollection>();
        _transactionalProducers
            .GetOrCreateAsync(Arg.Any<string>(), Arg.Any<KafkaProducerConfiguration>(), Arg.Any<IOutboundEnvelope>(), Arg.Any<IKafkaTransaction>())
            .Returns(ValueTask.FromResult(_innerProducer));

        _producer = new KafkaTransactionalProducer("producer1", _configuration, _transactionalProducers);
    }

    public enum EnvelopeProduceMethod
    {
        Produce,
        ProduceWithCallbacks,
        ProduceWithCallbacksAndState,
        ProduceAsync
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        _producer.Name.ShouldBe("producer1");
        _producer.DisplayName.ShouldBe("producer1");
        _producer.Configuration.ShouldBeSameAs(_configuration);
        _producer.EndpointConfiguration.ShouldBeSameAs(_configuration.Endpoints.Single());
    }

    [Theory]
    [InlineData(EnvelopeProduceMethod.Produce)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacks)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacksAndState)]
    [InlineData(EnvelopeProduceMethod.ProduceAsync)]
    public async Task Produce_ShouldProduceViaTransactionalProducerAndBeginTransaction(EnvelopeProduceMethod method)
    {
        SilverbackContext context = CreateContext();
        IKafkaTransaction transaction = context.InitKafkaTransaction("suffix");
        IOutboundEnvelope envelope = CreateEnvelope([1, 2, 3], context);

        IBrokerMessageIdentifier? identifier = await ProduceAsync(envelope, method);

        identifier.ShouldBe(new KafkaOffset("topic1", 0, 42));
        _produced.ShouldHaveSingleItem().Value.ShouldBe([1, 2, 3]);
        await _transactionalProducers.Received(1).GetOrCreateAsync("producer1", _configuration, envelope, transaction);
        _client.Received(1).BeginTransaction();
    }

    [Theory]
    [InlineData(EnvelopeProduceMethod.Produce)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacks)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacksAndState)]
    [InlineData(EnvelopeProduceMethod.ProduceAsync)]
    public async Task Produce_ShouldBindTransactionToProducer(EnvelopeProduceMethod method)
    {
        SilverbackContext context = CreateContext();
        IKafkaTransaction transaction = context.InitKafkaTransaction("suffix");
        IOutboundEnvelope envelope = CreateEnvelope([1, 2, 3], context);

        await ProduceAsync(envelope, method);
        transaction.Commit();

        _client.Received(1).CommitTransaction();
    }

    [Theory]
    [InlineData(EnvelopeProduceMethod.Produce)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacks)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacksAndState)]
    [InlineData(EnvelopeProduceMethod.ProduceAsync)]
    public async Task Produce_ShouldBeginTransactionOnlyOnce_WhenProducingMultipleMessages(EnvelopeProduceMethod method)
    {
        SilverbackContext context = CreateContext();
        context.InitKafkaTransaction("suffix");

        await ProduceAsync(CreateEnvelope([1], context), method);
        await ProduceAsync(CreateEnvelope([2], context), method);

        _produced.Count.ShouldBe(2);
        _client.Received(1).BeginTransaction();
    }

    [Theory]
    [InlineData(EnvelopeProduceMethod.Produce)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacks)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacksAndState)]
    [InlineData(EnvelopeProduceMethod.ProduceAsync)]
    public async Task Produce_ShouldThrow_WhenEnvelopeHasNoContext(EnvelopeProduceMethod method)
    {
        IOutboundEnvelope envelope = CreateEnvelope([1, 2, 3], null);

        Func<Task> act = () => ProduceAsync(envelope, method);

        await act.ShouldThrowAsync<MissingKafkaTransactionException>();
        _produced.ShouldBeEmpty();
        await _transactionalProducers.DidNotReceive().GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<KafkaProducerConfiguration>(),
            Arg.Any<IOutboundEnvelope>(),
            Arg.Any<IKafkaTransaction>());
    }

    [Theory]
    [InlineData(EnvelopeProduceMethod.Produce)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacks)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacksAndState)]
    [InlineData(EnvelopeProduceMethod.ProduceAsync)]
    public async Task Produce_ShouldThrow_WhenNoTransactionInitialized(EnvelopeProduceMethod method)
    {
        SilverbackContext context = CreateContext();
        IOutboundEnvelope envelope = CreateEnvelope([1, 2, 3], context);

        Func<Task> act = () => ProduceAsync(envelope, method);

        await act.ShouldThrowAsync<InvalidOperationException>();
        _produced.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(EnvelopeProduceMethod.Produce)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacks)]
    [InlineData(EnvelopeProduceMethod.ProduceWithCallbacksAndState)]
    [InlineData(EnvelopeProduceMethod.ProduceAsync)]
    public async Task Produce_ShouldThrow_WhenTransactionIsBoundToAnotherProducer(EnvelopeProduceMethod method)
    {
        SilverbackContext context = CreateContext();
        context.InitKafkaTransaction("suffix");
        context.GetKafkaTransaction().BindConfluentProducer(Substitute.For<IConfluentProducerWrapper>());
        IOutboundEnvelope envelope = CreateEnvelope([1, 2, 3], context);

        Func<Task> act = () => ProduceAsync(envelope, method);

        Exception exception = await act.ShouldThrowAsync<InvalidOperationException>();
        exception.Message.ShouldBe("The transaction is already bound to a producer.");
        _produced.ShouldBeEmpty();
        _client.DidNotReceive().BeginTransaction();
    }

    [Theory]
    [InlineData("Produce(message)")]
    [InlineData("Produce(message, callbacks)")]
    [InlineData("Produce(message, callbacks, state)")]
    [InlineData("RawProduce(bytes)")]
    [InlineData("RawProduce(stream)")]
    [InlineData("RawProduce(bytes, callbacks)")]
    [InlineData("RawProduce(bytes, callbacks, state)")]
    [InlineData("RawProduce(stream, callbacks)")]
    [InlineData("RawProduce(stream, callbacks, state)")]
    [InlineData("ProduceAsync(message)")]
    [InlineData("RawProduceAsync(bytes)")]
    [InlineData("RawProduceAsync(stream)")]
    public async Task Produce_ShouldThrowNotSupported_WhenNotProducingEnvelope(string overload)
    {
        Func<Task> act = () => InvokeUnsupportedOverloadAsync(_producer, overload);

        Exception exception = await act.ShouldThrowAsync<NotSupportedException>();
        exception.Message.ShouldBe("Use the overload accepting an IOutboundEnvelope instead.");
        _produced.ShouldBeEmpty();
    }

    public void Dispose() => _innerProducer.Dispose();

    private static async Task InvokeUnsupportedOverloadAsync(KafkaTransactionalProducer producer, string overload)
    {
        byte[] bytes = [1, 2, 3];
        using MemoryStream stream = new(bytes);

        switch (overload)
        {
            case "Produce(message)":
                producer.Produce(new object());
                break;
            case "Produce(message, callbacks)":
                producer.Produce(new object(), null, _ => { }, _ => { });
                break;
            case "Produce(message, callbacks, state)":
                producer.Produce(new object(), null, (_, _) => { }, (_, _) => { }, 42);
                break;
            case "RawProduce(bytes)":
                producer.RawProduce(bytes);
                break;
            case "RawProduce(stream)":
                producer.RawProduce(stream);
                break;
            case "RawProduce(bytes, callbacks)":
                producer.RawProduce(bytes, null, _ => { }, _ => { });
                break;
            case "RawProduce(bytes, callbacks, state)":
                producer.RawProduce(bytes, null, (_, _) => { }, (_, _) => { }, 42);
                break;
            case "RawProduce(stream, callbacks)":
                producer.RawProduce(stream, null, _ => { }, _ => { });
                break;
            case "RawProduce(stream, callbacks, state)":
                producer.RawProduce(stream, null, (_, _) => { }, (_, _) => { }, 42);
                break;
            case "ProduceAsync(message)":
                await producer.ProduceAsync(new object());
                break;
            case "RawProduceAsync(bytes)":
                await producer.RawProduceAsync(bytes);
                break;
            case "RawProduceAsync(stream)":
                await producer.RawProduceAsync(stream);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(overload), overload, "Unknown overload.");
        }
    }

    private static IConfluentProducerWrapper CreateClient(
        KafkaProducerConfiguration configuration,
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced)
    {
        IConfluentProducerWrapper client = Substitute.For<IConfluentProducerWrapper>();
        client.Configuration.Returns(configuration);
        client
            .When(wrapper => wrapper.Produce(
                Arg.Any<TopicPartition>(),
                Arg.Any<Message<byte[]?, byte[]?>>(),
                Arg.Any<Action<DeliveryReport<byte[]?, byte[]?>>>()))
            .Do(call =>
            {
                Message<byte[]?, byte[]?> message = call.Arg<Message<byte[]?, byte[]?>>();
                produced.Enqueue(message);
                call.Arg<Action<DeliveryReport<byte[]?, byte[]?>>>().Invoke(
                    new DeliveryReport<byte[]?, byte[]?>
                    {
                        Message = message,
                        Error = new Error(ErrorCode.NoError),
                        TopicPartitionOffset = new TopicPartitionOffset("topic1", 0, 42),
                        Status = PersistenceStatus.Persisted
                    });
            });
        client
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Message<byte[]?, byte[]?> message = call.Arg<Message<byte[]?, byte[]?>>();
                produced.Enqueue(message);

                return new DeliveryResult<byte[]?, byte[]?>
                {
                    Message = message,
                    TopicPartitionOffset = new TopicPartitionOffset("topic1", 0, 42),
                    Status = PersistenceStatus.Persisted
                };
            });

        return client;
    }

    private static SilverbackContext CreateContext()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISilverbackLogger<IProducer>)).Returns(Substitute.For<ISilverbackLogger<IProducer>>());

        return new SilverbackContext(serviceProvider);
    }

    private OutboundEnvelope CreateEnvelope(byte[] content, ISilverbackContext? context) =>
        new(content, null, _configuration.Endpoints.Single(), _producer, context);

    private Task<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, EnvelopeProduceMethod method) =>
        Task.Run(async () => method switch
        {
            EnvelopeProduceMethod.Produce => _producer.Produce(envelope),
            EnvelopeProduceMethod.ProduceWithCallbacks => await ProduceWithCallbacksAsync(envelope).ConfigureAwait(false),
            EnvelopeProduceMethod.ProduceWithCallbacksAndState => await ProduceWithCallbacksAndStateAsync(envelope).ConfigureAwait(false),
            _ => await _producer.ProduceAsync(envelope).ConfigureAwait(false)
        }).WaitAsync(Timeout);

    private Task<IBrokerMessageIdentifier?> ProduceWithCallbacksAsync(IOutboundEnvelope envelope)
    {
        TaskCompletionSource<IBrokerMessageIdentifier?> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _producer.Produce(envelope, taskCompletionSource.SetResult, taskCompletionSource.SetException);

        return taskCompletionSource.Task;
    }

    private Task<IBrokerMessageIdentifier?> ProduceWithCallbacksAndStateAsync(IOutboundEnvelope envelope)
    {
        TaskCompletionSource<IBrokerMessageIdentifier?> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _producer.Produce(
            envelope,
            static (identifier, state) => state.SetResult(identifier),
            static (exception, state) => state.SetException(exception),
            taskCompletionSource);

        return taskCompletionSource.Task;
    }
}
