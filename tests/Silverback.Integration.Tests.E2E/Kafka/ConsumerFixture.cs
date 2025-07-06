// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ConsumerFixture : KafkaFixture
{
    public ConsumerFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task PauseAndResume_ShouldPauseAndResumeConsuming()
    {
        int[] receivedMessages = [0, 0, 0];
        TopicPartition pausedPartition = new(DefaultTopicName, 1);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IInboundEnvelope<TestEventWithKafkaKey>>(HandleEnvelope));

        void HandleEnvelope(IInboundEnvelope<TestEventWithKafkaKey> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            receivedMessages[offset.TopicPartition.Partition]++;
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.Pause([pausedPartition]);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Sum() == 6);
        receivedMessages.Sum().ShouldBe(6);
        receivedMessages[pausedPartition.Partition].ShouldBe(0); // First + message ready in queue

        consumer.Resume([pausedPartition]);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Sum().ShouldBe(9);
        receivedMessages[0].ShouldBe(3);
        receivedMessages[1].ShouldBe(3);
        receivedMessages[2].ShouldBe(3);
    }

    [Fact]
    public async Task Seek_ShouldRepositionOffset()
    {
        int[] receivedMessages = [0, 0, 0];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IInboundEnvelope<TestEventWithKafkaKey>>(HandleEnvelope));

        void HandleEnvelope(IInboundEnvelope<TestEventWithKafkaKey> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            receivedMessages[offset.TopicPartition.Partition]++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Sum().ShouldBe(9);
        receivedMessages[0].ShouldBe(3);
        receivedMessages[1].ShouldBe(3);
        receivedMessages[2].ShouldBe(3);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.Seek(new TopicPartitionOffset(DefaultTopicName, 1, 0));

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Sum() >= 12);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Sum().ShouldBe(12);
        receivedMessages[1].ShouldBe(6);
    }

    [Fact]
    public async Task StopAsync_ShouldCancelProcessing()
    {
        bool received = false;
        bool canceled = false;
        using CancellationTokenSource antiDeadlockCancellationTokenSource = new();
        using SemaphoreSlim semaphore = new(0);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<TestEventOne, CancellationToken>(HandleEventAsync));

        async Task HandleEventAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            received = true;

            while (!cancellationToken.IsCancellationRequested && !antiDeadlockCancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(10, CancellationToken.None);
            }

            canceled = true;

            await semaphore.WaitAsync(CancellationToken.None);
        }

        try
        {
            KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();

            IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
            await producer.ProduceAsync(new TestEventOne(), cancellationToken: CancellationToken.None);

            await AsyncTestingUtil.WaitAsync(() => received);
            received.ShouldBeTrue();

            consumer.StopAsync().FireAndForget();

            await AsyncTestingUtil.WaitAsync(() => canceled);
            canceled.ShouldBeTrue();
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);

            semaphore.Release();
            await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);
            consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        }
        finally
        {
            antiDeadlockCancellationTokenSource.Cancel();
            semaphore.Release(); // in any case, always release to avoid deadlocking
        }
    }
}
