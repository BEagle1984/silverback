// Copyright (c) 2025 Sergio Aquilini
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
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenStillFailingAfterRetries()
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
                        .OnError(policy => policy.Retry(10).ThenSkip()))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenChunkedJsonStillFailingAfterRetries()
    {
        int tryCount = 0;
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne());

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
                        .OnError(policy => policy.Retry(10).ThenSkip()))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithKafkaKey("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeadersWithKafkaKey("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeadersWithKafkaKey("1", 2, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenBatchStillFailingAfterRetries()
    {
        int tryCount = 0;

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
                        .EnableBatchProcessing(3)
                        .OnError(policy => policy.Retry(10).ThenSkip()))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<IMessageStreamEnumerable<IIntegrationEvent>>(HandleBatch));

        void HandleBatch(IMessageStreamEnumerable<IIntegrationEvent> batch)
        {
            List<IIntegrationEvent> dummy = [.. batch]; // Enumerate
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);
    }

    [Fact]
    public async Task SkipPolicy_ShouldRetryBatchAndSkipMessage_WhenJsonDeserializationFailsInBatch()
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
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(5)
                        .OnError(policy => policy.Retry(3).ThenSkip()))))
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

        // Produce 3 valid messages, 1 invalid message, 3 valid messages, 1 invalid message and then 4 valid messages (10 valid messages in total)
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        for (int i = 0; i < 3; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
        }

        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        for (int i = 0; i < 3; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
        }

        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        for (int i = 0; i < 4; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(8);
        completedBatches.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(3);
        receivedBatches[1].Count.ShouldBe(3);
        receivedBatches[2].Count.ShouldBe(3);
        receivedBatches[3].Count.ShouldBe(5);
        receivedBatches[4].Count.ShouldBe(1);
        receivedBatches[5].Count.ShouldBe(1);
        receivedBatches[6].Count.ShouldBe(1);
        receivedBatches[7].Count.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(12);
    }
}
