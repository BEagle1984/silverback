// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Formatter;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class ErrorHandlingTests : MqttTestFixture
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task RetryPolicy_ProcessingRetriedMultipleTimes()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry(10))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_PriorV500_ProcessingRetriedIndefinitely()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            configuration => configuration
                                .WithClientId("e2e-test")
                                .ConnectViaTcp("e2e-mqtt-broker")
                                .UseProtocolVersion(MqttProtocolVersion.V311))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound<TestEventOne>(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry())))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await AsyncTestingUtil.WaitAsync(() => tryCount > 15);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        tryCount.Should().BeGreaterThan(15);
    }

    [Fact]
    public async Task RetryPolicy_SuccessfulAfterSomeTries_OffsetCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry(10))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(3);
        Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task RetryPolicy_StillFailingAfterRetries_MessageNotCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry(10))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task RetryPolicy_StillFailingAfterRetries_ConsumerStopped()
    {
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry(10))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await AsyncTestingUtil.WaitAsync(() => !Helper.Broker.Consumers[0].IsConnected);

        tryCount.Should().Be(11);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task RetryAndSkipPolicies_StillFailingAfterRetries_MessageCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry(10).ThenSkip())))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task RetryAndSkipPolicies_StillFailingAfterRetriesWithV3_MessageCommitted()
    {
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            configuration => configuration
                                .WithClientId("e2e-test")
                                .ConnectViaTcp("e2e-mqtt-broker")
                                .UseProtocolVersion(MqttProtocolVersion.V311))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .DeserializeJson(
                                    serializer =>
                                        serializer.UseFixedType<TestEventOne>())
                                .OnError(policy => policy.Retry(10).ThenSkip())))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.Should().Be(11);
        Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task SkipPolicy_JsonDeserializationError_MessageSkipped()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Skip())))
                .AddIntegrationSpy());

        MqttProducer producer = Helper.Broker.GetProducer(
            producer => producer
                .ProduceTo(DefaultTopicName)
                .ConfigureClient(
                    configuration => configuration
                        .WithClientId("e2e-test")
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .UseProtocolVersion(MqttProtocolVersion.V500)));
        await producer.RawProduceAsync(
            invalidRawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
        Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);

        await producer.RawProduceAsync(
            rawMessage,
            new MessageHeaderCollection
            {
                { "x-message-type", typeof(TestEventOne).AssemblyQualifiedName }
            });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task RetryPolicy_EncryptedMessage_RetriedMultipleTimes()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        Stream rawMessageStream = DefaultSerializers.Json.Serialize(message);
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(
                            producer => producer
                                .ProduceTo(DefaultTopicName)
                                .EncryptUsingAes(AesEncryptionKey))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.Retry(10))
                                .DecryptUsingAes(AesEncryptionKey)))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReadAll().Should().NotBeEquivalentTo(rawMessageStream.ReReadAll());
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task MovePolicy_ToOtherTopic_MessageMoved()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(policy => policy.MoveToMqttTopic(moveEndpoint => moveEndpoint.ProduceTo("e2e/other")))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        static void HandleMessage(IIntegrationEvent message) => throw new InvalidOperationException("Move!");

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

        Helper.Spy.OutboundEnvelopes[1].Message.Should()
            .BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("e2e/other");

        IReadOnlyList<MqttApplicationMessage> otherTopicMessages = Helper.GetMessages("e2e/other");
        otherTopicMessages.Count.Should().Be(1);
        otherTopicMessages[0].Payload.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }

    [Fact]
    public async Task MovePolicy_ToSameTopic_MessageMovedAndRetried()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(
                                    policy => policy.MoveToMqttTopic(
                                        moveEndpoint => moveEndpoint.ProduceTo(DefaultTopicName),
                                        movePolicy => movePolicy.WithMaxRetries(10)))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(11);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
    }

    [Fact]
    public async Task MovePolicy_ToOtherTopicAfterRetry_MessageRetriedAndMoved()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };
        int tryCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .OnError(
                                    policy => policy
                                        .Retry(1)
                                        .ThenMoveToMqttTopic(moveEndpoint => moveEndpoint.ProduceTo("e2e/other")))))
                .AddIntegrationSpy()
                .AddDelegateSubscriber2<IIntegrationEvent>(HandleMessage));

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        tryCount.Should().Be(2);

        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("e2e/other");

        IReadOnlyList<MqttApplicationMessage> otherTopicMessages = Helper.GetMessages("e2e/other");
        otherTopicMessages.Count.Should().Be(1);
        otherTopicMessages[0].Payload.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }
}
