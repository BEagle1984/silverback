// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task SkipPolicy_ShouldSkipMessage_WhenJsonDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
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
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipSequence_WhenChunkedJsonDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!><what?!><what?!>");

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip()))))
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            invalidRawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            invalidRawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            invalidRawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);

        await producer.RawProduceAsync(
            rawMessage.Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("2", 2, true, typeof(TestEventOne)));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(6);
    }

    [Fact]
    public async Task SkipPolicy_ShouldSkipSequence_WhenJsonBatchDeserializationFails()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);
        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");
        List<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

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
                                .Consume(
                                    endpoint => endpoint
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

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().BeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);

        for (int i = 0; i < 6; i++)
        {
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
                });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 6);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(5);
        receivedBatches[1].Should().HaveCount(1);
        completedBatches.Should().Be(1);

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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(5);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(12);
    }
}
