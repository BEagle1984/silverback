// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

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

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
    }

    [Fact]
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenChunkedJsonStillFailingAfterRetries()
    {
        int tryCount = 0;
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne());

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
            HeadersHelper.GetChunkHeaders("1", 0, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(10).Take(10).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 1, typeof(TestEventOne)));
        await producer.RawProduceAsync(
            rawMessage.Skip(20).ToArray(),
            HeadersHelper.GetChunkHeaders("1", 2, true, typeof(TestEventOne)));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task RetryAndSkipPolicies_ShouldSkip_WhenBatchStillFailingAfterRetries()
    {
        int tryCount = 0;

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
                                        .EnableBatchProcessing(3)
                                        .OnError(policy => policy.Retry(10).ThenSkip()))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber<IMessageStreamEnumerable<IIntegrationEvent>>(HandleBatch));

        void HandleBatch(IMessageStreamEnumerable<IIntegrationEvent> batch)
        {
            List<IIntegrationEvent> dummy = batch.ToList(); // Enumerate
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }
}
