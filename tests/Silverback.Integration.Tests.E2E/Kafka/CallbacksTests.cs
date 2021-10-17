// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
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
            callbackHandlers.Cast<SimpleCallbackHandler>().Should()
                .OnlyContain(handler => handler.CallCount == 1);
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

        [Fact]
        public async Task PartitionEofCallback_PartitionEofDisabled_HandlerNotInvoked()
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
                        .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
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
                                                config.EnablePartitionEof = false;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            var callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
                .ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .First(service => service is KafkaPartitionEofCallback);

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(0);
            kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        }

        [Fact]
        public async Task PartitionEofCallback_PartitionEofEnabled_HandlerInvoked()
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
                        .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
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
                                                config.EnablePartitionEof = true;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
                .ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .First(service => service is KafkaPartitionEofCallback);

            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 5);

            var kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 6);

            // There are 5 partitions and one message will be published, so in fact 6 times the end of the partition should be reached
            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(6);
            kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        }

        [Fact]
        public async Task PartitionEofCallback_PartitionEofEnabledForMultipleConsumers_HandlerInvoked()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
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
                                                config.EnablePartitionEof = true;
                                            }))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer2";
                                                config.EnablePartitionEof = true;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
                .ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .First(service => service is KafkaPartitionEofCallback);

            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 10);

            // There are 5 partitions and 2 consumers
            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(10);
        }

        [Fact]
        public async Task PartitionEofCallback_PublishSeveralMessages_HandlerInvoked()
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
                        .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(2)))
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
            var callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
                .ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .First(service => service is KafkaPartitionEofCallback);

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 2);

            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(2);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventOne);
            await publisher.PublishAsync(messageEventTwo);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 4);

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(2);

            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(4);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventOne);
            await publisher.PublishAsync(messageEventTwo);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 6);

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(4);
            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(6);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventOne);
            await publisher.PublishAsync(messageEventTwo);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 8);

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(6);
            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(8);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventOne);
            await publisher.PublishAsync(messageEventTwo);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 10);

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(8);
            callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(10);
        }

        [Fact]
        public async Task PartitionEofCallback_PublishSeveralMessages_HandlerInvokedForRightPartition()
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
                        .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(
                                    mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(2)))
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
            var callbackHandlerKafkaPartitionEofCallback = (KafkaPartitionEofCallback)Host
                .ScopedServiceProvider
                .GetServices<IBrokerCallback>()
                .First(service => service is KafkaPartitionEofCallback);

            var topicPartition0 = new TopicPartition(DefaultTopicName, new Partition(0));
            var topicPartition1 = new TopicPartition(DefaultTopicName, new Partition(1));

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount == 2);

            callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount.Should().Be(2);
            callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition0).Should()
                .Be(1);
            callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition1).Should()
                .Be(1);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventOne);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount == 4);

            kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(1);

            callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount.Should().Be(4);
            callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition0).Should()
                .Be(2);
            callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition1).Should()
                .Be(2);

            await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

            await publisher.PublishAsync(messageEventTwo);

            await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

            await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(
                () => callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount == 6);

            callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount.Should().Be(6);
            callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition0).Should()
                .Be(3);
            callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition1).Should()
                .Be(3);
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
        private sealed class AnotherSimpleCallbackHandler : SimpleCallbackHandler
        {
        }

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "ClassNeverInstantiated.Local",
            Justification = Justifications.CalledBySilverback)]
        private sealed class MessageSendingCallbackHandler : IEndpointsConfiguredCallback
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

        [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage(
            "ReSharper",
            "ClassNeverInstantiated.Local",
            Justification = Justifications.CalledBySilverback)]
        private sealed class KafkaPartitionEofCallback : IKafkaPartitionEofCallback
        {
            public int AllPartitionsEofCallbackCount =>
                PartitionEofCallbacksDictionary.Sum(pair => pair.Value);

            private ConcurrentDictionary<TopicPartition, int> PartitionEofCallbacksDictionary { get; } =
                new();

            public void OnEndOfTopicPartitionReached(TopicPartition topicPartition, KafkaConsumer consumer)
            {
                PartitionEofCallbacksDictionary.AddOrUpdate(topicPartition, 1, (_, value) => value + 1);
            }

            public int GetPartitionEofCallbackCount(TopicPartition topicPartition)
            {
                return PartitionEofCallbacksDictionary[topicPartition];
            }
        }
    }
}
