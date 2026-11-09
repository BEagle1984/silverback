// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenAsyncEnumerableProcessingFails()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne message in enumerable)
            {
                receivedMessages.Add(message);
                if (receivedMessages.Count == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Count.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenObservableProcessingFails()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .AsObservable()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamObservable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamObservable<TestEventOne> observable) =>
            observable.Subscribe(message =>
            {
                receivedMessages.Add(message);

                if (receivedMessages.Count == 2)
                    throw new InvalidOperationException("Test");
            });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Count.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Streaming_ShouldApplyErrorPolicyOnceToFailedMessage_WhenProcessingFails(int failingMessageNumber)
    {
        List<TestEventOne> receivedMessages = [];
        StopCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddSingletonErrorPolicyCallback(callback));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne message in enumerable)
            {
                receivedMessages.Add(message);

                if (receivedMessages.Count == failingMessageNumber)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        receivedMessages.Count.ShouldBe(failingMessageNumber);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(failingMessageNumber - 1);

        IReadOnlyList<(string? Content, Exception Exception)> stoppingCalls = callback.StoppingCalls;
        stoppingCalls.Count.ShouldBe(1);
        stoppingCalls[0].Content.ShouldBe($"{failingMessageNumber}");
        stoppingCalls[0].Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Test");
    }

    private sealed class StopCallback : IStopConsumerErrorPolicyCallback
    {
        private readonly List<(string? Content, Exception Exception)> _stoppingCalls = [];

        public IReadOnlyList<(string? Content, Exception Exception)> StoppingCalls
        {
            get
            {
                lock (_stoppingCalls)
                {
                    return [.. _stoppingCalls];
                }
            }
        }

        public Task OnStoppingConsumerAsync(IRawInboundEnvelope envelope, Exception exception)
        {
            lock (_stoppingCalls)
            {
                _stoppingCalls.Add((((envelope as IInboundEnvelope)?.Message as TestEventOne)?.ContentEventOne, exception));
            }

            return Task.CompletedTask;
        }
    }
}
