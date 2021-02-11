// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class KafkaBrokerEventsTests : KafkaTestFixture
    {
        public KafkaBrokerEventsTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task BrokerEventsRegistered_EndpointsConfiguredHandlerCalled()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddSingletonBrokerEventsHandler<SimpleBrokerEventsHandler>()
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
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
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

            var eventsHandler = (SimpleBrokerEventsHandler)Host.ServiceProvider.GetRequiredService<IBrokerEventsHandler>();
            eventsHandler.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task BrokerEventsRegistered_AllEndpointsConfiguredHandlersCalled()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddSingletonBrokerEventsHandler<SimpleBrokerEventsHandler>()
                        .AddSingletonBrokerEventsHandler<AnotherSimpleBrokerEventsHandler>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
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

            var eventsHandlers = Host.ServiceProvider.GetServices<IBrokerEventsHandler>().ToList();
            eventsHandlers.Should().HaveCount(2);
            eventsHandlers.Should().AllBeAssignableTo<SimpleBrokerEventsHandler>();
            eventsHandlers.Cast<SimpleBrokerEventsHandler>().Should().OnlyContain(b => b.CallCount == 1);
        }

        [Fact]
        public async Task BrokerEventsRegistered_MessagePublished()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddScopedBrokerEventsHandler<MessageSendingBrokerEventsHandler>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
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

        private class SimpleBrokerEventsHandler : BrokerEventsHandler
        {
            public int CallCount { get; private set; }

            public override Task OnEndpointsConfiguredAsync()
            {
                CallCount++;
                return Task.CompletedTask;
            }
        }

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
        private class AnotherSimpleBrokerEventsHandler : SimpleBrokerEventsHandler
        {
        }

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
        private class MessageSendingBrokerEventsHandler : BrokerEventsHandler
        {
            private readonly IPublisher _publisher;

            public MessageSendingBrokerEventsHandler(IPublisher publisher)
            {
                _publisher = publisher;
            }

            public override async Task OnEndpointsConfiguredAsync()
            {
                var message = new TestEventOne();
                await _publisher.PublishAsync(message);
            }
        }
    }
}
