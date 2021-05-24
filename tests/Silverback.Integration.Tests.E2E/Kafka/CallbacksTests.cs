// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class CallbacksTests : KafkaTestFixture
    {
        public CallbacksTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task EndpointsConfiguredCallback_Invoked()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddSingletonBrokerCallbackHandler<SimpleCallbackHandler>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventTwo>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))))
                .Run();

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
            await kafkaTestingHelper.WaitUntilConnectedAsync();

            var callbackHandler = (SimpleCallbackHandler)Host.ServiceProvider
                .GetServices<IBrokerCallback>()
                .Single(service => service is SimpleCallbackHandler);
            callbackHandler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task EndpointsConfiguredCallback_AllHandlersInvoked()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddSingletonBrokerCallbackHandler<SimpleCallbackHandler>()
                        .AddSingletonBrokerCallbackHandler<AnotherSimpleCallbackHandler>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventTwo>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            }))))
                .Run();

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
            await kafkaTestingHelper.WaitUntilConnectedAsync();

            var callbackHandlers = Host.ServiceProvider
                .GetServices<IBrokerCallback>()
                .Where(service => service is SimpleCallbackHandler)
                .ToList();
            callbackHandlers.Should().HaveCount(2);
            callbackHandlers.Cast<SimpleCallbackHandler>().Should().OnlyContain(handler => handler.CallCount == 1);
        }

        [Fact]
        public async Task EndpointsConfiguredCallback_MessagePublished()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddScopedBrokerCallbackHandler<MessageSendingCallbackHandler>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        }

        private class SimpleCallbackHandler : IEndpointsConfiguredCallback
        {
            public int CallCount { get; private set; }

            public Task OnEndpointsConfiguredAsync()
            {
                CallCount++;
                return Task.CompletedTask;
            }
        }

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "ClassNeverInstantiated.Local",
            Justification = Justifications.CalledBySilverback)]
        private class AnotherSimpleCallbackHandler : SimpleCallbackHandler
        {
        }

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "ClassNeverInstantiated.Local",
            Justification = Justifications.CalledBySilverback)]
        private class MessageSendingCallbackHandler : IEndpointsConfiguredCallback
        {
            private readonly IPublisher _publisher;

            public MessageSendingCallbackHandler(IPublisher publisher)
            {
                _publisher = publisher;
            }

            public async Task OnEndpointsConfiguredAsync()
            {
                var message = new TestEventOne();
                await _publisher.PublishAsync(message);
            }
        }
    }
}
