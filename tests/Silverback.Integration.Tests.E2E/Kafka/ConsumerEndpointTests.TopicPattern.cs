// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointTests
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeFromTopicsMatchingPattern()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("^topic-[a-z]+$"))))
            .AddIntegrationSpyAndSubscriber());

        await CreateTopicsAsync("topic-a", "topic-b", "topic-1", "other");

        IProducer producerA = GetProducer("topic-a");
        IProducer producerB = GetProducer("topic-b");
        IProducer producer1 = GetProducer("topic-1");
        IProducer producerOther = GetProducer("other");

        for (int i = 1; i <= 3; i++)
        {
            await producerA.ProduceAsync(new TestEventOne { ContentEventOne = $"a{i}" });
            await producerB.ProduceAsync(new TestEventOne { ContentEventOne = $"b{i}" });
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"1-{i}" });
            await producerOther.ProduceAsync(new TestEventOne { ContentEventOne = $"other{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope<TestEventOne>[] envelopes = [.. Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>()];
        envelopes.Length.ShouldBe(6);
        envelopes.ShouldAllBe(envelope => envelope.Endpoint.Configuration.RawName == "^topic-[a-z]+$");
        envelopes
            .Where(envelope => envelope.Endpoint.RawName == "topic-a")
            .Select(envelope => envelope.Message?.ContentEventOne)
            .ShouldBe(["a1", "a2", "a3"], true);
        envelopes
            .Where(envelope => envelope.Endpoint.RawName == "topic-b")
            .Select(envelope => envelope.Message?.ContentEventOne)
            .ShouldBe(["b1", "b2", "b3"], true);
    }

    [Theory]
    [InlineData(PartitionAssignmentStrategy.Range)]
    [InlineData(PartitionAssignmentStrategy.CooperativeSticky)]
    public async Task ConsumerEndpoint_ShouldConsumeFromTopicCreatedAfterConsuming_WhenMatchingPattern(
        PartitionAssignmentStrategy assignmentStrategy)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .WithPartitionAssignmentStrategy(assignmentStrategy)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("^topic-"))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producerA = GetProducer("topic-a");

        await producerA.ProduceAsync(new TestEventOne { ContentEventOne = "a1" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);

        IProducer producerB = GetProducer("topic-b");

        await producerB.ProduceAsync(new TestEventOne { ContentEventOne = "b1" });
        await producerA.ProduceAsync(new TestEventOne { ContentEventOne = "a2" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        // The rebalance triggered by the new topic can cause a message to be processed again (at-least-once delivery)
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne?)envelope.Message)?.ContentEventOne)
            .Distinct()
            .ShouldBe(["a1", "b1", "a2"], true);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic-b").ShouldBe(1);
        Helper.GetConsumerGroup(DefaultGroupId).GetCommittedOffsetsCount("topic-b").ShouldBe(1);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldRouteToTopicNameEndpoint_WhenTopicAlsoMatchesPattern()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("pattern", endpoint => endpoint.ConsumeFrom("^topic-"))
                    .Consume<TestEventTwo>("name", endpoint => endpoint.ConsumeFrom("topic-b"))))
            .AddIntegrationSpyAndSubscriber());

        await CreateTopicsAsync("topic-a");

        IProducer producerA = GetProducer("topic-a");
        IProducer producerB = Helper.GetProducerForEndpoint("name");

        await producerA.ProduceAsync(new TestEventOne { ContentEventOne = "a1" });
        await producerB.ProduceAsync(new TestEventTwo { ContentEventTwo = "b1" });
        await producerA.ProduceAsync(new TestEventOne { ContentEventOne = "a2" });
        await producerB.ProduceAsync(new TestEventTwo { ContentEventTwo = "b2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope<TestEventOne>[] eventOneEnvelopes = [.. Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventOne>>()];
        IInboundEnvelope<TestEventTwo>[] eventTwoEnvelopes = [.. Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<TestEventTwo>>()];

        eventOneEnvelopes.Select(envelope => envelope.Message?.ContentEventOne).ShouldBe(["a1", "a2"], true);
        eventOneEnvelopes.ShouldAllBe(envelope => envelope.Endpoint.RawName == "topic-a" && envelope.Endpoint.Configuration.FriendlyName == "pattern");
        eventTwoEnvelopes.Select(envelope => envelope.Message?.ContentEventTwo).ShouldBe(["b1", "b2"], true);
        eventTwoEnvelopes.ShouldAllBe(envelope => envelope.Endpoint.RawName == "topic-b" && envelope.Endpoint.Configuration.FriendlyName == "name");
    }

    [Theory]
    [InlineData("mixed", new[] { "other-x", "topic-a", "topic-b" })]
    [InlineData("^topic-[a-z]$", new[] { "topic-a", "topic-b" })]
    [InlineData("other-x", new[] { "other-x" })]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitForTopicsOfEndpoint_WhenWaitingForEndpointNameOrSubscription(
        string endpointName,
        string[] expectedProcessedTopics)
    {
        ConcurrentDictionary<string, int> processedCounts = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("mixed", endpoint => endpoint.ConsumeFrom("other-x", "^topic-[a-z]$"))))
            .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(async Task (envelope) =>
            {
                await Task.Delay(10);
                processedCounts.AddOrUpdate(envelope.Endpoint.RawName, 1, (_, count) => count + 1);
            }));

        IProducer producerX = GetProducer("other-x");
        IProducer producerA = GetProducer("topic-a");
        IProducer producerB = GetProducer("topic-b");

        for (int i = 1; i <= 5; i++)
        {
            await producerX.ProduceAsync(new TestEventOne());
            await producerA.ProduceAsync(new TestEventOne());
            await producerB.ProduceAsync(new TestEventOne());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync(endpointName);

        // The rebalances triggered by the new topics can cause some messages to be processed again (at-least-once delivery)
        expectedProcessedTopics.Select(topic => processedCounts.GetValueOrDefault(topic)).ShouldAllBe(count => count >= 5);
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToTopicName_WhenEndpointSubscribesPatternFirst()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("mixed", endpoint => endpoint.ConsumeFrom("^topic-[a-z]$", "topic-x"))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint("mixed");
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe("topic-x");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToTopicMatchingPattern()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("pattern", endpoint => endpoint.ConsumeFrom("^topic-"))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint("topic-a");
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "a" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        envelope.Endpoint.RawName.ShouldBe("topic-a");
        envelope.Endpoint.Configuration.FriendlyName.ShouldBe("pattern");
        envelope.Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("a");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldPreferEndpointSubscribingTopicName_WhenTopicAlsoMatchesPattern()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .AddConsumer(consumer => consumer
                    .WithBootstrapServers("PLAINTEXT://e2e-1")
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("pattern", endpoint => endpoint.ConsumeFrom("^topic-")))
                .AddConsumer(consumer => consumer
                    .WithBootstrapServers("PLAINTEXT://e2e-2")
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("name", endpoint => endpoint.ConsumeFrom("topic-b", "topic-c"))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint("topic-b");
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.Configuration.FriendlyName.ShouldBe("name");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToTopicMatchingPattern_WhenFriendlyNameIsTopicName()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("topic-a", endpoint => endpoint.ConsumeFrom("^topic-"))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint("topic-a");
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe("topic-a");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenEndpointOnlySubscribesTopicPattern()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>("pattern", endpoint => endpoint.ConsumeFrom("^topic-"))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint("pattern");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldStartWith("Cannot produce to the endpoint 'pattern (^topic-)', since it only subscribes topic patterns.");
    }

    private IProducer GetProducer(string topic) =>
        Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(topic)));

    // Creating the topics upfront avoids the rebalances (and the possible duplicates) while consuming
    private async Task CreateTopicsAsync(params string[] topics)
    {
        foreach (string topic in topics)
        {
            Helper.GetTopic(topic, "PLAINTEXT://e2e");
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
    }
}
