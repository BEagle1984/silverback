// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
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

        [Theory]
        [InlineData(true, 6)] // There are 5 partitions and one message will be published, so in fact 6 times the end of the partition will be reached
        [InlineData(false, 0)]
        public async Task EndpointsConfiguredKafkaEndOfTopicPartitionReachedCallback(bool enablePartitionEof, int expectedEndOfPartitionReachedCount)
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
                        .AddSingletonBrokerCallbackHandler<KafkaEndOfTopicPartitionReachedCallback>()
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
                                                config.EnablePartitionEof = enablePartitionEof;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            var callbackHandlers = Host.ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .Where(service => service is KafkaEndOfTopicPartitionReachedCallback)
                .ToList();

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            callbackHandlers.Should().HaveCount(1);
            callbackHandlers.Cast<KafkaEndOfTopicPartitionReachedCallback>().Should().OnlyContain(handler => handler.EndOfPartitionReachedCount == expectedEndOfPartitionReachedCount);

            kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        }

        [Fact]
        public async Task EndpointsConfiguredKafkaEndOfTopicPartitionReachedCallback_WithProducingSeveralMessages()
        {
            var messageEventOne = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var messageEventTwo = new TestEventTwo
            {
                Content = "Hello E2E!"
            };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddSingletonBrokerCallbackHandler<KafkaEndOfTopicPartitionReachedCallback>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(options => options.WithDefaultPartitionsCount(2)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName, 0))
                                .AddOutbound<TestEventTwo>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName, 1))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnablePartitionEof = true;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            var callbackHandlers = Host.ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .Where(service => service is KafkaEndOfTopicPartitionReachedCallback)
                .ToList();

            callbackHandlers.Should().HaveCount(1);
            callbackHandlers.Cast<KafkaEndOfTopicPartitionReachedCallback>().Should().OnlyContain(handler => handler.EndOfPartitionReachedCount == 2);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventOne);
            await publisher.PublishAsync(messageEventTwo);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(2);
            callbackHandlers.Cast<KafkaEndOfTopicPartitionReachedCallback>().Should().OnlyContain(handler => handler.EndOfPartitionReachedCount == 4);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            for (var i = 0; i < 2; i++)
            {
                await publisher.PublishAsync(messageEventOne);
                await publisher.PublishAsync(messageEventTwo);
            }

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(6);
            callbackHandlers.Cast<KafkaEndOfTopicPartitionReachedCallback>().Should().OnlyContain(handler => handler.EndOfPartitionReachedCount == 6);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            for (var i = 0; i < 3; i++)
            {
                await publisher.PublishAsync(messageEventOne);
                await publisher.PublishAsync(messageEventTwo);
            }

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(12);
            callbackHandlers.Cast<KafkaEndOfTopicPartitionReachedCallback>().Should().OnlyContain(handler => handler.EndOfPartitionReachedCount == 8);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            for (var i = 0; i < 5; i++)
            {
                await publisher.PublishAsync(messageEventOne);
                await publisher.PublishAsync(messageEventTwo);
            }

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(22);
            callbackHandlers.Cast<KafkaEndOfTopicPartitionReachedCallback>().Should().OnlyContain(handler => handler.EndOfPartitionReachedCount == 10);
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
        private class KafkaEndOfTopicPartitionReachedCallback : IKafkaEndOfTopicPartitionReachedCallback
        {
            private readonly object _lockObject = new();

            public int EndOfPartitionReachedCount { get; private set; }

            public void OnEndOfTopicPartitionReached(TopicPartition topicPartition, KafkaConsumer consumer)
            {
                lock (_lockObject)
                {
                    EndOfPartitionReachedCount++;
                }
            }
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
