// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.Filter;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class InboundMessageFilterTests : KafkaTests
{
    public InboundMessageFilterTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Filter_ShouldSkipAndCommitFilteredMessages()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Filter(message => message?.ContentEventOne != "discard"))))
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedContents.ShouldBe(["1", "2", "3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task Filter_ShouldNotStopConsumer_WhenFilteredMessageHasNoSubscriber()
    {
        TestingCollection<IIntegrationEvent> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .ThrowIfUnhandled()
                        .Filter(message => message is not TestEventTwo))))
            .AddDelegateSubscriber<TestEventOne>(receivedMessages.Add));

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "unhandled" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "unhandled" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.Select(message => ((TestEventOne)message).ContentEventOne).ShouldBe(["1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task Filter_ShouldSkipAndCommitFilteredEnvelopes()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Filter(envelope => envelope.Headers.GetValue("x-tenant") == "my-tenant"))))
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(
            new TestEventOne { ContentEventOne = "1" },
            new MessageHeaderCollection { { "x-tenant", "my-tenant" } });
        await producer.ProduceAsync(
            new TestEventOne { ContentEventOne = "2" },
            new MessageHeaderCollection { { "x-tenant", "other-tenant" } });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(
            new TestEventOne { ContentEventOne = "4" },
            new MessageHeaderCollection { { "x-tenant", "my-tenant" } });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedContents.ShouldBe(["1", "4"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldResolveFilterFromMessageScope()
    {
        TestingCollection<(string? Content, string? FilteredContent)> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<MessageScopeState>()
            .AddScoped<ScopedContentFilter>()
            .AddSingleton<FilterInvocations>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Filter<ScopedContentFilter>())))
            .AddDelegateSubscriber<TestEventOne, MessageScopeState>(HandleMessage));

        // The subscriber receives the scoped state that the filter used, if both are resolved from the scope of the message
        void HandleMessage(TestEventOne message, MessageScopeState state) =>
            receivedMessages.Add((message.ContentEventOne, state.FilteredContent));

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe([("1", "1"), ("2", "2"), ("3", "3")]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        FilterInvocations invocations = Host.ServiceProvider.GetRequiredService<FilterInvocations>();
        invocations.States.Select(state => state.FilteredContent).ShouldBe(["1", "discard", "2", "discard", "3"]);
        invocations.States.Distinct().Count().ShouldBe(5);
    }

    [Fact]
    public async Task Filter_ShouldSkipFilteredMessagesInBatch()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        int evaluatedCount = 0;

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
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .Filter(
                            message =>
                            {
                                Interlocked.Increment(ref evaluatedCount);
                                return message?.ContentEventOne != "discard";
                            }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        // The filtered message was processed before the next one was added to the pending batch: it must not commit the batch
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count == 1 && receivedBatches[0].Count == 2);
        receivedBatches.ShouldHaveSingleItem().ShouldBe(["1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "4" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "5" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "6" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["1", "2", "3"]);
        receivedBatches[1].ShouldBe(["4", "5", "6"]);
        evaluatedCount.ShouldBe(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(11);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldContinueConsuming_WhenAllMessagesInBatchAreFiltered()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        int evaluatedCount = 0;

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
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .Filter(
                            message =>
                            {
                                Interlocked.Increment(ref evaluatedCount);
                                return message?.ContentEventOne != "discard";
                            }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        }

        await AsyncTestingUtil.WaitAsync(() => evaluatedCount == 4);
        evaluatedCount.ShouldBe(4);
        receivedBatches.SelectMany(batch => batch).ShouldBeEmpty();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldSkipFilteredMessagesInBatch_WhenPartitionsProcessedTogether()
    {
        TestingCollection<List<string?>> receivedBatches = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .ProcessAllPartitionsTogether()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(4)
                        .Filter(message => message?.ContentEventOne?.EndsWith(":discard", StringComparison.Ordinal) != true))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        // The content starts with the target partition
        IProducer producer = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint
                .ProduceTo(
                    DefaultTopicName,
                    message => int.Parse(message!.ContentEventOne!.Split(':')[0], CultureInfo.InvariantCulture))));

        // The last message of each partition is accepted, so that the commit of the last batch covers all the offsets
        string[] contents =
        [
            "0:1", "1:discard", "2:2", "0:discard", "1:3", "2:discard",
            "0:4", "1:discard", "2:5", "0:6", "1:7", "2:8"
        ];

        foreach (string content in contents)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = content });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches.ShouldAllBe(batch => batch.Count == 4);
        receivedBatches.SelectMany(batch => batch).ShouldBe(["0:1", "2:2", "1:3", "0:4", "2:5", "0:6", "1:7", "2:8"], true);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(12);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldSkipAndCommitFilteredChunkedMessages()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(10)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Filter(message => message?.ContentEventOne != "Long message to discard"))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message to discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message to discard" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedContents.ShouldBe(["Long message 1"]);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["Long message 1"]);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(6);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldSkipAndCommitFilteredChunkedMessagesInBatch()
    {
        TestingCollection<List<string?>> receivedBatches = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .EnableChunking(10)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .Filter(message => message?.ContentEventOne != "Long message to discard"))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        // The filtered chunked messages are linked to the pending batch (or start it), without being part of it. The last message is
        // accepted, so that the commit of the last batch covers all the offsets.
        string[] contents =
        [
            "Long message to discard", "Long message 1", "Long message to discard", "Long message 2", "Long message 3",
            "Long message to discard", "Long message 4", "Long message to discard", "Long message 5", "Long message 6"
        ];

        foreach (string content in contents)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = content });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["Long message 1", "Long message 2", "Long message 3"]);
        receivedBatches[1].ShouldBe(["Long message 4", "Long message 5", "Long message 6"]);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(20);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(Helper.Spy.RawOutboundEnvelopes.Count);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldSkipFilteredMessages_WhenStreaming()
    {
        TestingCollection<string?> receivedContents = [];

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
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .AllowStreaming()
                        .Filter(message => message?.ContentEventOne != "discard"))))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            foreach (TestEventOne message in stream)
            {
                receivedContents.Add(message.ContentEventOne);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedContents.ShouldBe(["1", "2", "3"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(7);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldApplyErrorPolicy_WhenFilterThrows()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Filter(message => message?.ContentEventOne != "boom" ? true : throw new InvalidOperationException("Test"))
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "boom" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedContents.ShouldBe(["1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldEvaluateFilterAgain_WhenRetrying()
    {
        TestingCollection<string?> receivedContents = [];
        ConcurrentQueue<string?> evaluatedContents = new();
        int failedCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .Filter(
                            message =>
                            {
                                evaluatedContents.Enqueue(message?.ContentEventOne);
                                return message?.ContentEventOne != "discard";
                            })
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        // The first accepted message fails once, the filtered messages are evaluated again by the retry of the failed one only
        void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne == "1" && Interlocked.Increment(ref failedCount) == 1)
                throw new InvalidOperationException("Test");

            receivedContents.Add(message.ContentEventOne);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "discard" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedContents.ShouldBe(["1", "2"]);
        evaluatedContents.ShouldBe(["1", "1", "discard", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    private sealed class MessageScopeState
    {
        public string? FilteredContent { get; set; }
    }

    private sealed class FilterInvocations
    {
        private readonly ConcurrentQueue<MessageScopeState> _states = new();

        public IReadOnlyCollection<MessageScopeState> States => _states;

        public void Add(MessageScopeState state) => _states.Enqueue(state);
    }

    private sealed class ScopedContentFilter : IInboundMessageFilter
    {
        private readonly MessageScopeState _state;

        private readonly FilterInvocations _invocations;

        public ScopedContentFilter(MessageScopeState state, FilterInvocations invocations)
        {
            _state = state;
            _invocations = invocations;
        }

        public async ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken)
        {
            await Task.Yield();

            _state.FilteredContent = (envelope.Message as TestEventOne)?.ContentEventOne;
            _invocations.Add(_state);

            return _state.FilteredContent != "discard";
        }
    }
}
