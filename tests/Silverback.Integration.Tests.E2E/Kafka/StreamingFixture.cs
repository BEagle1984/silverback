// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class StreamingFixture : KafkaFixture
{
    public StreamingFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndCommit_WhenSubscribingToStream()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            foreach (TestEventOne message in stream)
            {
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndCommit_WhenSubscribingToEnvelopesStream()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>> stream)
        {
            foreach (IInboundEnvelope<TestEventOne> envelope in stream)
            {
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(receivedMessages.Count);
                receivedMessages.Add(envelope.Message!);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndCommit_WhenSubscribingToStreamAndEnumeratingAsynchronously()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne message in stream)
            {
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndCommit_WhenSubscribingToEnumerable()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IEnumerable<TestEventOne> stream)
        {
            foreach (TestEventOne message in stream)
            {
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndCommit_WhenSubscribingToAsyncEnumerable()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne message in stream)
            {
                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(receivedMessages.Count);
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }

    [Fact]
    public async Task Streaming_ShouldConsumeAndCommit_WhenSubscribingToObservable()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AsObservable()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamObservable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamObservable<TestEventOne> stream) =>
            stream.Subscribe(
                message =>
                {
                    DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(receivedMessages.Count);
                    receivedMessages.Add(message);
                });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(15);
        receivedMessages.Select(message => message.ContentEventOne)
            .ShouldBe(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }
}
