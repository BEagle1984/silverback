// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BasicTests : KafkaTestFixture
{
    public BasicTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Outbound_DefaultSettings_SerializedAndProduced()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName)))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
    }

    [Fact]
    public async Task Outbound_AllowDuplicateEndpoints_SerializedAndProducedTwice()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AllowDuplicateEndpointRegistrations())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(rawMessage);
    }

    [Fact]
    public async Task OutboundAndInbound_DefaultSettings_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(
                            producer => producer
                                .ProduceTo(DefaultTopicName)
                                .WithName("OUT"))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .WithName("IN")
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboundAndInbound_DefaultSettings_MessagesNotOverlapping()
    {
        int[] receivedMessages = { 0, 0, 0 };
        int[] exitedSubscribers = { 0, 0, 0 };
        bool areOverlapping = false;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddDelegateSubscriber2<IInboundEnvelope<TestEventOne>>(HandleEnvelope));

        async ValueTask HandleEnvelope(IInboundEnvelope<TestEventOne> envelope)
        {
            KafkaOffset offset = (KafkaOffset)envelope.BrokerMessageIdentifier;
            int partitionIndex = offset.TopicPartition.Partition;

            if (receivedMessages[partitionIndex] != exitedSubscribers[partitionIndex])
                areOverlapping = true;

            receivedMessages[partitionIndex]++;

            await Task.Delay(100);

            exitedSubscribers[partitionIndex]++;
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        areOverlapping.Should().BeFalse();
        receivedMessages.Sum().Should().Be(10);
        exitedSubscribers.Sum().Should().Be(10);
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleTopicsForDifferentMessages_ProducedAndConsumed()
    {
        TestingCollection<IEvent> receivedEvents = new();
        TestingCollection<TestEventOne> receivedTestEventOnes = new();
        TestingCollection<TestEventTwo> receivedTestEventTwos = new();

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<TestEventOne>(producer => producer.ProduceTo("topic1"))
                        .AddOutbound<TestEventTwo>(producer => producer.ProduceTo("topic2"))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom("topic1")
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId)))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom("topic2")
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddDelegateSubscriber2<IInboundEnvelope<IEvent>>(HandleEnvelope)
                .AddDelegateSubscriber2<TestEventOne>(HandleEventOne)
                .AddDelegateSubscriber2<TestEventTwo>(HandleEventTwo));

        void HandleEnvelope(IInboundEnvelope<IEvent> envelope) => receivedEvents.Add(envelope.Message!);
        void HandleEventOne(TestEventOne message) => receivedTestEventOnes.Add(message);
        void HandleEventTwo(TestEventTwo message) => receivedTestEventTwos.Add(message);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { Content = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedEvents.Should().HaveCount(10);
        receivedTestEventOnes.Should().HaveCount(5);
        receivedTestEventTwos.Should().HaveCount(5);

        DefaultConsumerGroup.GetCommittedOffsetsCount("topic1").Should().Be(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount("topic2").Should().Be(5);
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleTopics_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo("topic1"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo("topic2"))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom("topic1")
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId)))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom("topic2")
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        IEnumerable<string?> receivedContentsTopic1 = Helper.Spy.InboundEnvelopes
            .Where(envelope => envelope.Endpoint.RawName == "topic1")
            .Select(envelope => ((TestEventOne)envelope.Message!).Content);
        IEnumerable<string?> receivedContentsTopic2 = Helper.Spy.InboundEnvelopes
            .Where(envelope => envelope.Endpoint.RawName == "topic2")
            .Select(envelope => ((TestEventOne)envelope.Message!).Content);

        List<string> expectedMessages = Enumerable.Range(1, 5).Select(i => $"{i}").ToList();

        receivedContentsTopic1.Should().BeEquivalentTo(expectedMessages);
        receivedContentsTopic2.Should().BeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleTopicsWithSingleConsumer_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo("topic1"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo("topic2"))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom("topic1", "topic2")
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

        IEnumerable<string?> receivedContentsTopic1 = Helper.Spy.InboundEnvelopes
            .Where(envelope => envelope.Endpoint.RawName == "topic1")
            .Select(envelope => ((TestEventOne)envelope.Message!).Content);
        IEnumerable<string?> receivedContentsTopic2 = Helper.Spy.InboundEnvelopes
            .Where(envelope => envelope.Endpoint.RawName == "topic2")
            .Select(envelope => ((TestEventOne)envelope.Message!).Content);

        List<string> expectedMessages =
            Enumerable.Range(1, 5).Select(i => $"{i}").ToList();

        receivedContentsTopic1.Should().BeEquivalentTo(expectedMessages);
        receivedContentsTopic2.Should().BeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleConsumersSameConsumerGroup_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId)))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Distinct()
            .Should().BeEquivalentTo(Enumerable.Range(1, 10).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleConsumersDifferentConsumerGroup_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId("group1")))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId("group2"))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(20);

        Helper.GetConsumerGroup("group1").GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
        Helper.GetConsumerGroup("group2").GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleConsumerInstances_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId)),
                            2))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Distinct()
            .Should().BeEquivalentTo(Enumerable.Range(1, 10).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Inbound_WithAndWithoutAutoCommit_OffsetCommitted(bool enableAutoCommit)
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(
                                    configuration => configuration with
                                    {
                                        GroupId = DefaultGroupId,
                                        EnableAutoCommit = enableAutoCommit,
                                        CommitOffsetEach = enableAutoCommit ? -1 : 3
                                    })))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task OutboundAndInbound_MessageWithCustomHeaders_HeadersTransferred()
    {
        TestEventWithHeaders message = new()
        {
            Content = "Hello E2E!",
            CustomHeader = "Hello header!",
            CustomHeader2 = false
        };

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddIntegrationSpy());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        Helper.Spy.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header", "Hello header!"));
        Helper.Spy.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header2", "False"));
    }

    [Fact]
    public async Task Inbound_ThrowIfUnhandled_ConsumerStoppedIfMessageIsNotHandled()
    {
        int received = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                .ThrowIfUnhandled()))
                .AddDelegateSubscriber2<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => received++;

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "Handled message" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(1);

        await publisher.PublishAsync(new TestEventTwo { Content = "Unhandled message" });

        await AsyncTestingUtil.WaitAsync(() => Helper.Broker.Consumers[0].IsConnected == false);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Inbound_IgnoreUnhandledMessages_UnhandledMessageIgnored()
    {
        int received = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                .IgnoreUnhandledMessages()))
                .AddDelegateSubscriber2<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => received++;

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "Handled message" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(1);

        await publisher.PublishAsync(new TestEventTwo { Content = "Unhandled message" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task DisconnectAsync_WithoutAutoCommit_PendingOffsetsCommitted()
    {
        int receivedMessages = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(
                                    configuration => configuration
                                        .WithGroupId(DefaultGroupId)
                                        .DisableAutoCommit()
                                        .CommitOffsetEach(10))))
                .AddDelegateSubscriber2<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => receivedMessages++;

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });

        await AsyncTestingUtil.WaitAsync(() => receivedMessages == 3);

        await Helper.Broker.DisconnectAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task StopAsyncAndStartAsync_DefaultSettings_MessagesConsumedAfterRestart()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        await Helper.Broker.Consumers[0].StopAsync();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Task.Delay(200);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);

        await Helper.Broker.Consumers[0].StartAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
    }

    [Fact]
    public async Task Inbound_FromMultiplePartitionsWithLimitedParallelism_ConcurrencyLimited()
    {
        List<TestEventWithKafkaKey> receivedMessages = new();
        TaskCompletionSource<bool> taskCompletionSource = new();

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))
                                .LimitParallelism(2)))
                .AddDelegateSubscriber2<TestEventWithKafkaKey>(HandleMessage));

        async Task HandleMessage(TestEventWithKafkaKey message)
        {
            lock (receivedMessages)
            {
                receivedMessages.Add(message);
            }

            await taskCompletionSource.Task;
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 4, Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
        await Task.Delay(100);

        try
        {
            receivedMessages.Should().HaveCount(2);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(12);
    }
}
