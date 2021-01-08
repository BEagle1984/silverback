// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class ErrorHandlingTests : MqttTestFixture
    {
        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c,
            0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        public ErrorHandlingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task RetryPolicy_ProcessingRetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            tryCount.Should().Be(11);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
            Helper.Spy.InboundEnvelopes.ForEach(
                envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task RetryPolicy_PriorV500_ProcessingRetriedIndefinitely()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker")
                                        .UseProtocolVersion(MqttProtocolVersion.V311))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DeserializeJson(
                                            serializer => serializer
                                                .UseFixedType<TestEventOne>())
                                        .OnError(policy => policy.Retry())))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await AsyncTestingUtil.WaitAsync(() => tryCount > 15);

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            tryCount.Should().BeGreaterThan(15);
        }

        [Fact]
        public async Task RetryPolicy_SuccessfulAfterSomeTries_OffsetCommitted()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(3);
            Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task RetryPolicy_StillFailingAfterRetries_MessageNotCommitted()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(11);
            Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(1);
        }

        [Fact]
        public async Task RetryPolicy_StillFailingAfterRetries_ConsumerStopped()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            await AsyncTestingUtil.WaitAsync(() => !Helper.Broker.Consumers[0].IsConnected);

            tryCount.Should().Be(11);
            Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task RetryAndSkipPolicies_StillFailingAfterRetries_MessageCommitted()
        {
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10).ThenSkip())))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Hello E2E!"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            tryCount.Should().Be(11);
            Helper.GetClientSession("e2e-test").PendingMessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task SkipPolicy_JsonDeserializationError_MessageSkipped()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

            byte[] invalidRawMessage = Encoding.UTF8.GetBytes("<what?!>");

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Skip())))
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new MqttProducerEndpoint(DefaultTopicName)
                {
                    Configuration =
                        ((MqttClientConfigBuilder)new MqttClientConfigBuilder()
                            .WithClientId("e2e-test")
                            .ConnectViaTcp("e2e-mqtt-broker")
                            .UseProtocolVersion(MqttProtocolVersion.V500))
                        .Build()
                });
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
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var rawMessageStream = await Endpoint.DefaultSerializer.SerializeAsync(
                message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10))
                                        .DecryptUsingAes(AesEncryptionKey)))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                if (tryCount != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].RawMessage.ReadAll().Should()
                .NotBeEquivalentTo(rawMessageStream.ReReadAll());
            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
            Helper.Spy.InboundEnvelopes.ForEach(
                envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task MovePolicy_ToOtherTopic_MessageMoved()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy.MoveToMqttTopic(
                                                moveEndpoint => moveEndpoint.ProduceTo("e2e/other")))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) => throw new InvalidOperationException("Move!")))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

            Helper.Spy.OutboundEnvelopes[1].Message.Should()
                .BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
            Helper.Spy.OutboundEnvelopes[1].Endpoint.Name.Should().Be("e2e/other");

            Helper.GetMessagesCount("e2e/other").Should().Be(1);
        }

        [Fact]
        public async Task MovePolicy_ToSameTopic_MessageMovedAndRetried()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy.MoveToMqttTopic(
                                                moveEndpoint => moveEndpoint.ProduceTo(DefaultTopicName),
                                                movePolicy => movePolicy.MaxFailedAttempts(10)))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(11);
            tryCount.Should().Be(11);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
            Helper.Spy.InboundEnvelopes.ForEach(
                envelope => envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task MovePolicy_ToOtherTopicAfterRetry_MessageRetriedAndMoved()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy
                                                .Retry(1)
                                                .ThenMoveToMqttTopic(
                                                    moveEndpoint => moveEndpoint.ProduceTo("e2e/other")))))
                        .AddIntegrationSpy()
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _) =>
                            {
                                tryCount++;
                                throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

            tryCount.Should().Be(2);

            Helper.Spy.OutboundEnvelopes[1].Message.Should()
                .BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
            Helper.Spy.OutboundEnvelopes[1].Endpoint.Name.Should().Be("e2e/other");

            Helper.GetMessagesCount("e2e/other").Should().Be(1);
        }
    }
}
