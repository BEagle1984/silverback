// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class BasicTests : MqttTestFixture
    {
        public BasicTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task OutboundAndInbound_DefaultSettings_ProducedAndConsumed()
        {
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
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
            Helper.Spy.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
        }

        [Fact]
        public async Task OutboundAndInbound_DefaultSettingsV311_ProducedAndConsumed()
        {
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
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
            Helper.Spy.InboundEnvelopes
                .Select(envelope => ((TestEventOne)envelope.Message!).Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
        }

        [Fact]
        public async Task OutboundAndInbound_MessageWithCustomHeaders_HeadersTransferred()
        {
            var message = new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
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
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
            Helper.Spy.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header", "Hello header!"));
            Helper.Spy.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header2", "False"));
        }

        [Fact]
        public async Task OutboundAndInbound_MultipleClients_ProducedAndConsumed()
        {
            var client1MessagesCount = 0;
            var client2MessagesCount = 0;

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
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(config => config.WithClientId("client1")))
                                .AddInbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(config => config.WithClientId("client2"))))
                        .AddDelegateSubscriber(
                            (IRawInboundEnvelope envelope) =>
                            {
                                var mqttEndpoint = (MqttConsumerEndpoint)envelope.Endpoint;

                                switch (mqttEndpoint.Configuration.ClientId)
                                {
                                    case "client1":
                                        client1MessagesCount++;
                                        break;
                                    case "client2":
                                        client2MessagesCount++;
                                        break;
                                    default:
                                        throw new InvalidOperationException();
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            client1MessagesCount.Should().Be(5);
            client2MessagesCount.Should().Be(5);
        }

        [Fact]
        public async Task Inbound_WithLimitedParallelism_ConcurrencyLimited()
        {
            var receivedMessages = new List<TestEventOne>();
            var taskCompletionSource = new TaskCompletionSource<bool>();

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
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(config => config.WithClientId("client1"))
                                        .LimitParallelism(2)))
                        .AddDelegateSubscriber(
                            async (TestEventOne message) =>
                            {
                                lock (receivedMessages)
                                {
                                    receivedMessages.Add(message);
                                }

                                await taskCompletionSource.Task;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 12; i++)
            {
                await publisher.PublishAsync(new TestEventOne());
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
}
