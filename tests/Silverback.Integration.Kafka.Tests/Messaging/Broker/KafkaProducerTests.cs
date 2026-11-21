// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.IO;
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
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public class KafkaProducerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

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

    private static KafkaProducer CreateProducer(ConcurrentQueue<Message<byte[]?, byte[]?>> produced)
    {
        KafkaProducerConfiguration configuration = new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .Produce<object>(endpoint => endpoint.ProduceTo("topic"))
            .Build();

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
                        TopicPartitionOffset = new TopicPartitionOffset("topic", 0, 42),
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
                    TopicPartitionOffset = new TopicPartitionOffset("topic", 0, 42),
                    Status = PersistenceStatus.Persisted
                };
            });

        IBrokerBehaviorsProvider<IProducerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>();
        behaviors.GetBehaviorsList().Returns([]);

        return new KafkaProducer(
            "test-producer",
            client,
            configuration,
            behaviors,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());
    }
}
