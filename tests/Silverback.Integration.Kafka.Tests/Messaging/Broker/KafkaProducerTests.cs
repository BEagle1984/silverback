// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public class KafkaProducerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public enum ProduceMode
    {
        Sync,
        Async,
        Callbacks
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawProduce_ShouldNotShareByteArray(bool useCallbacks)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);
        byte[] content = [1, 2, 3];

        await RawProduceAsync(producer, content, useCallbacks);

        Message<byte[]?, byte[]?> message = produced.ShouldHaveSingleItem();
        message.Value.ShouldBe([1, 2, 3]);
        message.Value.ShouldNotBeSameAs(content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawProduce_ShouldNotShareStreamBuffer(bool useCallbacks)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);
        byte[] content = [1, 2, 3];

        await RawProduceAsync(producer, new MemoryStream(content), useCallbacks);

        Message<byte[]?, byte[]?> message = produced.ShouldHaveSingleItem();
        message.Value.ShouldBe([1, 2, 3]);
        message.Value.ShouldNotBeSameAs(content);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawProduce_ShouldNotCopyOwnedBuffer(bool useCallbacks)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);
        byte[] buffer = [1, 2, 3];

        await RawProduceAsync(producer, new OwnedReadOnlyMemoryStream(buffer), useCallbacks);

        produced.ShouldHaveSingleItem().Value.ShouldBeSameAs(buffer);
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldReturnKafkaOffset(ProduceMode mode)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);

        IBrokerMessageIdentifier? identifier = await RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        identifier.ShouldBe(new KafkaOffset("topic", 0, 42));
        produced.ShouldHaveSingleItem().Value.ShouldBe([1, 2, 3]);
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldMapHeaders(ProduceMode mode)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);

        await RawProduceWithModeAsync(producer, [1, 2, 3], [new MessageHeader("one", "1"), new MessageHeader("two", "2")], mode);

        Message<byte[]?, byte[]?> message = produced.ShouldHaveSingleItem();
        message.Headers.ShouldNotBeNull();
        message.Headers.Count.ShouldBe(2);
        Encoding.UTF8.GetString(message.Headers.GetLastBytes("one")).ShouldBe("1");
        Encoding.UTF8.GetString(message.Headers.GetLastBytes("two")).ShouldBe("2");
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldNotSetHeaders_WhenNoHeaders(ProduceMode mode)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);

        await RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        Message<byte[]?, byte[]?> message = produced.ShouldHaveSingleItem();
        message.Headers.ShouldBeNull();
        message.Key.ShouldBeNull();
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldSetKafkaKey_WhenMessageKeyHeaderIsSet(ProduceMode mode)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);

        await RawProduceWithModeAsync(producer, [1, 2, 3], [new MessageHeader(KafkaMessageHeaders.MessageKey, "key1")], mode);

        Message<byte[]?, byte[]?> message = produced.ShouldHaveSingleItem();
        message.Key.ShouldBe(Encoding.UTF8.GetBytes("key1"));
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldNotSetKafkaKey_WhenMessageKeyHeaderIsNull(ProduceMode mode)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        using KafkaProducer producer = CreateProducer(produced);

        await RawProduceWithModeAsync(producer, [1, 2, 3], [new MessageHeader(KafkaMessageHeaders.MessageKey, null)], mode);

        Message<byte[]?, byte[]?> message = produced.ShouldHaveSingleItem();
        message.Key.ShouldBeNull();
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    public async Task RawProduce_ShouldThrowProduceException_WhenClientThrowsKafkaException(ProduceMode mode)
    {
        KafkaException kafkaException = new(ErrorCode.Local_QueueFull);
        IConfluentProducerWrapper client = Substitute.For<IConfluentProducerWrapper>();
        client
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(kafkaException);
        using KafkaProducer producer = CreateProducer(CreateConfiguration(), client);

        Task act = RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        ProduceException exception = await act.ShouldThrowAsync<ProduceException>();
        exception.InnerException.ShouldBeSameAs(kafkaException);
    }

    [Fact]
    public async Task RawProduce_ShouldThrowProduceException_WhenDeliveryReportHasError()
    {
        IConfluentProducerWrapper client = CreateClient([], PersistenceStatus.NotPersisted, ErrorCode.Local_MsgTimedOut);
        using KafkaProducer producer = CreateProducer(CreateConfiguration(), client);

        Task act = RawProduceWithModeAsync(producer, [1, 2, 3], null, ProduceMode.Callbacks);

        Exception exception = await act.ShouldThrowAsync<ProduceException>();
        exception.Message.ShouldBe("Error occurred producing the message. (error code Local_MsgTimedOut)");
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldThrow_WhenPossiblyPersistedAndThrowIfNotAcknowledged(ProduceMode mode)
    {
        IConfluentProducerWrapper client = CreateClient([], PersistenceStatus.PossiblyPersisted);
        using KafkaProducer producer = CreateProducer(CreateConfiguration(), client);

        Task act = RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        Exception exception = await act.ShouldThrowAsync<ProduceException>();
        exception.Message.ShouldBe("The message was transmitted to broker, but no acknowledgement was received.");
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldLogWarning_WhenPossiblyPersistedAndNotThrowIfNotAcknowledged(ProduceMode mode)
    {
        LoggerSubstitute<KafkaProducer> loggerSubstitute = new(LogLevel.Trace);
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        IConfluentProducerWrapper client = CreateClient(produced, PersistenceStatus.PossiblyPersisted);
        using KafkaProducer producer = CreateProducer(
            CreateConfiguration(builder => builder.IgnoreIfNotAcknowledged()),
            client,
            new SilverbackLogger<KafkaProducer>(new MappedLevelsLogger<KafkaProducer>([], loggerSubstitute)));

        IBrokerMessageIdentifier? identifier = await RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        identifier.ShouldBe(new KafkaOffset("topic", 0, 42));
        produced.ShouldHaveSingleItem();
        loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Message produced to topic[0] but not acknowledged | ProducerName: test-producer",
            2022);
    }

    [Theory]
    [InlineData(ProduceMode.Sync, true)]
    [InlineData(ProduceMode.Async, true)]
    [InlineData(ProduceMode.Callbacks, true)]
    [InlineData(ProduceMode.Sync, false)]
    [InlineData(ProduceMode.Async, false)]
    [InlineData(ProduceMode.Callbacks, false)]
    public async Task RawProduce_ShouldThrow_WhenNotPersisted(ProduceMode mode, bool throwIfNotAcknowledged)
    {
        IConfluentProducerWrapper client = CreateClient([], PersistenceStatus.NotPersisted);
        using KafkaProducer producer = CreateProducer(
            CreateConfiguration(
                builder =>
                {
                    if (throwIfNotAcknowledged)
                        builder.ThrowIfNotAcknowledged();
                    else
                        builder.IgnoreIfNotAcknowledged();
                }),
            client);

        Task act = RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        Exception exception = await act.ShouldThrowAsync<ProduceException>();
        exception.Message.ShouldStartWith("The message was never transmitted to the broker");
    }

    [Theory]
    [InlineData(ProduceMode.Sync)]
    [InlineData(ProduceMode.Async)]
    [InlineData(ProduceMode.Callbacks)]
    public async Task RawProduce_ShouldIgnorePersistenceStatus_WhenDeliveryReportsAreDisabled(ProduceMode mode)
    {
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced = new();
        IConfluentProducerWrapper client = CreateClient(produced, PersistenceStatus.NotPersisted);
        using KafkaProducer producer = CreateProducer(
            CreateConfiguration(builder => builder.DisableDeliveryReports().IgnoreIfNotAcknowledged()),
            client);

        IBrokerMessageIdentifier? identifier = await RawProduceWithModeAsync(producer, [1, 2, 3], null, mode);

        identifier.ShouldBe(new KafkaOffset("topic", 0, 42));
        produced.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RawProduceAsync_ShouldThrow_WhenDeliveryResultIsNull()
    {
        IConfluentProducerWrapper client = Substitute.For<IConfluentProducerWrapper>();
        client
            .ProduceAsync(Arg.Any<TopicPartition>(), Arg.Any<Message<byte[]?, byte[]?>>(), Arg.Any<CancellationToken>())
            .ReturnsNull();
        using KafkaProducer producer = CreateProducer(CreateConfiguration(), client);

        Task act = RawProduceWithModeAsync(producer, [1, 2, 3], null, ProduceMode.Async);

        Exception exception = await act.ShouldThrowAsync<ProduceException>();
        exception.Message.ShouldBe("The delivery report is null.");
    }

    private static Task RawProduceAsync(KafkaProducer producer, byte[] content, bool useCallbacks) =>
        Task.Run(async () =>
        {
            if (useCallbacks)
            {
                TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                producer.RawProduce(content, null, _ => taskCompletionSource.SetResult(), taskCompletionSource.SetException);
                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            else
            {
                await producer.RawProduceAsync(content).ConfigureAwait(false);
            }
        }).WaitAsync(Timeout);

    private static Task RawProduceAsync(KafkaProducer producer, Stream content, bool useCallbacks) =>
        Task.Run(async () =>
        {
            if (useCallbacks)
            {
                TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                producer.RawProduce(content, null, _ => taskCompletionSource.SetResult(), taskCompletionSource.SetException);
                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            else
            {
                await producer.RawProduceAsync(content).ConfigureAwait(false);
            }
        }).WaitAsync(Timeout);

    private static Task<IBrokerMessageIdentifier?> RawProduceWithModeAsync(
        KafkaProducer producer,
        byte[] content,
        IReadOnlyCollection<MessageHeader>? headers,
        ProduceMode mode) =>
        Task.Run(async () =>
        {
            switch (mode)
            {
                case ProduceMode.Sync:
                    return producer.RawProduce(content, headers);
                case ProduceMode.Async:
                    return await producer.RawProduceAsync(content, headers).ConfigureAwait(false);
                default:
                    TaskCompletionSource<IBrokerMessageIdentifier?> taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    producer.RawProduce(content, headers, taskCompletionSource.SetResult, taskCompletionSource.SetException);
                    return await taskCompletionSource.Task.ConfigureAwait(false);
            }
        }).WaitAsync(Timeout);

    private static KafkaProducer CreateProducer(ConcurrentQueue<Message<byte[]?, byte[]?>> produced) =>
        CreateProducer(CreateConfiguration(), CreateClient(produced, PersistenceStatus.Persisted));

    private static KafkaProducerConfiguration CreateConfiguration(Action<KafkaProducerConfigurationBuilder>? configure = null)
    {
        KafkaProducerConfigurationBuilder builder = new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .Produce<object>(endpoint => endpoint.ProduceTo("topic"));

        configure?.Invoke(builder);

        return builder.Build();
    }

    private static IConfluentProducerWrapper CreateClient(
        ConcurrentQueue<Message<byte[]?, byte[]?>> produced,
        PersistenceStatus persistenceStatus,
        ErrorCode deliveryReportErrorCode = ErrorCode.NoError)
    {
        IConfluentProducerWrapper client = Substitute.For<IConfluentProducerWrapper>();
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
                        Error = new Error(deliveryReportErrorCode),
                        TopicPartitionOffset = new TopicPartitionOffset("topic", 0, 42),
                        Status = persistenceStatus
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
                    TopicPartitionOffset = new TopicPartitionOffset("topic", 0, 42),
                    Status = persistenceStatus
                };
            });

        return client;
    }

    private static KafkaProducer CreateProducer(
        KafkaProducerConfiguration configuration,
        IConfluentProducerWrapper client,
        ISilverbackLogger<KafkaProducer>? logger = null)
    {
        client.Configuration.Returns(configuration);

        IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
        behaviors.GetBehaviorsList().Returns([]);

        return new KafkaProducer(
            "test-producer",
            client,
            configuration,
            behaviors,
            Substitute.For<IServiceProvider>(),
            logger ?? Substitute.For<ISilverbackLogger<KafkaProducer>>());
    }
}
