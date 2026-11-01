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
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesTests
{
    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenJsonDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = "<what?!>"u8.ToArray();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.InboundEnvelopes.ShouldBeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenSyncSubscriberThrowsApplicableException()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip(skipPolicy => skipPolicy.ApplyTo<ArgumentException>())))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new ArgumentException("Skip!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        tryCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
        Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipSequence_WhenChunkedJsonDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = "<what?!><what?!><what?!>"u8.ToArray();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            [.. invalidRawMessage.Take(10)],
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            [.. invalidRawMessage.Skip(10).Take(10)],
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            [.. invalidRawMessage.Skip(20)],
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.InboundEnvelopes.ShouldBeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);

        await producer.RawProduceAsync(
            [.. rawMessage.Take(10)],
            HeadersHelper.GetChunkHeaders("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(10).Take(10)],
            HeadersHelper.GetChunkHeaders("2", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            [.. rawMessage.Skip(20)],
            HeadersHelper.GetChunkHeaders("2", 2, true, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(6);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipMessage_WhenJsonDeserializationFailsInBatch()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);
        byte[] invalidRawMessage = "<what?!>"u8.ToArray();
        List<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1) // Commit immediately to be able to reliably test commit not happening
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(5)
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch)
            .AddIntegrationSpy());

        async Task HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne testEvent in batch)
            {
                list.Add(testEvent);
            }

            completedBatches++;
        }

        // Produce 10 valid messages in total
        // Starting with 3 valid messages, then 1 invalid message, then 1 valid message
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        for (int i = 0; i < 3; i++)
        {
            await producer.RawProduceAsync(rawMessage);
        }

        await producer.RawProduceAsync(invalidRawMessage);

        await producer.RawProduceAsync(rawMessage);

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count == 1 && receivedBatches[0].Count == 4);
        receivedBatches.Count.ShouldBe(1);
        receivedBatches[0].Count.ShouldBe(4);

        // Ensure nothing is committed yet
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);

        // Produce another 2 valid messages, then 1 invalid message and then 4 valid messages
        // (10 valid messages in total, 2 invalid)
        await producer.RawProduceAsync(rawMessage);
        await producer.RawProduceAsync(rawMessage);
        await producer.RawProduceAsync(invalidRawMessage);
        await producer.RawProduceAsync(rawMessage);
        await producer.RawProduceAsync(rawMessage);
        await producer.RawProduceAsync(rawMessage);
        await producer.RawProduceAsync(rawMessage);

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(5);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(12);
    }

    [Fact]
    public async Task SkipPolicy_ShouldInvokeCallbacks()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSingleton<SkippedMessagesStore>()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddScopedErrorPolicyCallback<SkippedMessagesCallback>()
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent message) => throw new InvalidOperationException("Skip!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        SkippedMessagesStore store = Host.ServiceProvider.GetRequiredService<SkippedMessagesStore>();
        store.Entries.Select(entry => ((TestEventOne)((IInboundEnvelope)entry.Envelope).Message!).ContentEventOne)
            .ShouldBe(["1", "2"], ignoreOrder: true);
        store.Entries.ShouldAllBe(entry => entry.Exception.Message == "Skip!");
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task SkipPolicy_ShouldStopConsumerWithoutCommitting_WhenCallbackThrows()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddSingletonErrorPolicyCallback<FailingSkipCallback>()
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent message) => throw new InvalidOperationException("Skip!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    private sealed class SkippedMessagesStore
    {
        private readonly List<(IRawInboundEnvelope Envelope, Exception Exception)> _entries = [];

        public IReadOnlyList<(IRawInboundEnvelope Envelope, Exception Exception)> Entries
        {
            get
            {
                lock (_entries)
                {
                    return [.. _entries];
                }
            }
        }

        public void Add(IRawInboundEnvelope envelope, Exception exception)
        {
            lock (_entries)
            {
                _entries.Add((envelope, exception));
            }
        }
    }

    private sealed class SkippedMessagesCallback : ISkipMessageErrorPolicyCallback
    {
        private readonly SkippedMessagesStore _store;

        public SkippedMessagesCallback(SkippedMessagesStore store)
        {
            _store = store;
        }

        public Task OnSkippingMessageAsync(IRawInboundEnvelope envelope, Exception exception)
        {
            _store.Add(envelope, exception);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingSkipCallback : ISkipMessageErrorPolicyCallback
    {
        public Task OnSkippingMessageAsync(IRawInboundEnvelope envelope, Exception exception) =>
            throw new InvalidOperationException("Callback failed!");
    }
}
