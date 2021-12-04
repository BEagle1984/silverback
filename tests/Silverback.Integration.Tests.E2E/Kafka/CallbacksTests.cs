// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

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
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<TestEventOne>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventTwo>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        }))))
            .Run();

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilConnectedAsync();

        SimpleCallbackHandler callbackHandler = (SimpleCallbackHandler)Host.ServiceProvider
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
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventTwo>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        }))))
            .Run();

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilConnectedAsync();

        List<IBrokerCallback> callbackHandlers = Host.ServiceProvider
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
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

        kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
    }

    [Fact]
    public async Task PartitionEofCallback_PartitionEofDisabled_HandlerNotInvoked()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnablePartitionEof = false;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        KafkaPartitionEofCallback callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
            .ScopedServiceProvider
            .GetServices<IBrokerCallback>()
            .First(service => service is KafkaPartitionEofCallback);

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();
        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(0);
        kafkaTestingHelper.Spy.OutboundEnvelopes.Should().HaveCount(1);
    }

    [Fact]
    public async Task PartitionEofCallback_PartitionEofEnabled_HandlerInvoked()
    {
        TestEventOne message = new() { Content = "Hello E2E!" };

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .AddSingletonBrokerCallbackHandler<KafkaPartitionEofCallback>()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnablePartitionEof = true;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaPartitionEofCallback callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
            .ScopedServiceProvider
            .GetServices<IBrokerCallback>()
            .First(service => service is KafkaPartitionEofCallback);

        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 5);

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 6);

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
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = "group1";
                                            configuration.EnablePartitionEof = true;
                                        }))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = "group2";
                                            configuration.EnablePartitionEof = true;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        KafkaPartitionEofCallback callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
            .ScopedServiceProvider
            .GetServices<IBrokerCallback>()
            .First(service => service is KafkaPartitionEofCallback);

        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 10);

        // There are 5 partitions and 2 consumers
        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(10);
    }

    [Fact]
    public async Task PartitionEofCallback_PublishSeveralMessages_HandlerInvoked()
    {
        TestEventOne messageEventOne = new()
        {
            Content = "Hello E2E!"
        };

        TestEventTwo messageEventTwo = new()
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
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(producer => producer.ProduceTo(DefaultTopicName, 0))
                            .AddOutbound<TestEventTwo>(producer => producer.ProduceTo(DefaultTopicName, 1))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnablePartitionEof = true;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        KafkaPartitionEofCallback callbackHandlerKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
            .ScopedServiceProvider
            .GetServices<IBrokerCallback>()
            .First(service => service is KafkaPartitionEofCallback);

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 2);

        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(2);

        await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

        await publisher.PublishAsync(messageEventOne);
        await publisher.PublishAsync(messageEventTwo);

        await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 4);

        kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(2);

        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(4);

        await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

        await publisher.PublishAsync(messageEventOne);
        await publisher.PublishAsync(messageEventTwo);

        await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 6);

        kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(4);
        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(6);

        await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

        await publisher.PublishAsync(messageEventOne);
        await publisher.PublishAsync(messageEventTwo);

        await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 8);

        kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(6);
        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(8);

        await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

        await publisher.PublishAsync(messageEventOne);
        await publisher.PublishAsync(messageEventTwo);

        await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 10);

        kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(8);
        callbackHandlerKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(10);
    }

    [Fact]
    public async Task PartitionEofCallback_PublishSeveralMessages_HandlerInvokedForRightPartition()
    {
        TestEventOne messageEventOne = new()
        {
            Content = "Hello E2E!"
        };

        TestEventTwo messageEventTwo = new()
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
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(producer => producer.ProduceTo(DefaultTopicName, 0))
                            .AddOutbound<TestEventTwo>(producer => producer.ProduceTo(DefaultTopicName, 1))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnablePartitionEof = true;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IKafkaTestingHelper kafkaTestingHelper = Host.ServiceProvider.GetRequiredService<IKafkaTestingHelper>();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        KafkaPartitionEofCallback callbackHandlerKafkaPartitionEofCallback = (KafkaPartitionEofCallback)Host
            .ScopedServiceProvider
            .GetServices<IBrokerCallback>()
            .First(service => service is KafkaPartitionEofCallback);

        TopicPartition topicPartition0 = new(DefaultTopicName, new Partition(0));
        TopicPartition topicPartition1 = new(DefaultTopicName, new Partition(1));

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount == 2);

        callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount.Should().Be(2);
        callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition0).Should().Be(1);
        callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition1).Should().Be(1);

        await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

        await publisher.PublishAsync(messageEventOne);

        await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount == 4);

        kafkaTestingHelper.Spy.InboundEnvelopes.Should().HaveCount(1);

        callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount.Should().Be(4);
        callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition0).Should().Be(2);
        callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition1).Should().Be(2);

        await kafkaTestingHelper.Broker.Consumers[0].DisconnectAsync();

        await publisher.PublishAsync(messageEventTwo);

        await kafkaTestingHelper.Broker.Consumers[0].ConnectAsync();

        await kafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount == 6);

        callbackHandlerKafkaPartitionEofCallback.AllPartitionsEofCallbackCount.Should().Be(6);
        callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition0).Should().Be(3);
        callbackHandlerKafkaPartitionEofCallback.GetPartitionEofCallbackCount(topicPartition1).Should().Be(3);
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
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    private sealed class AnotherSimpleCallbackHandler : SimpleCallbackHandler
    {
    }

    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    private sealed class MessageSendingCallbackHandler : IEndpointsConfiguredCallback
    {
        private readonly IPublisher _publisher;

        public MessageSendingCallbackHandler(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task OnEndpointsConfiguredAsync()
        {
            TestEventOne message = new();
            await _publisher.PublishAsync(message);
        }
    }

    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    private sealed class KafkaPartitionEofCallback : IKafkaPartitionEofCallback
    {
        public int AllPartitionsEofCallbackCount => PartitionEofCallbacksDictionary.Sum(pair => pair.Value);

        private ConcurrentDictionary<TopicPartition, int> PartitionEofCallbacksDictionary { get; } = new();

        public void OnEndOfTopicPartitionReached(TopicPartition topicPartition, KafkaConsumer consumer) =>
            PartitionEofCallbacksDictionary.AddOrUpdate(topicPartition, 1, (_, value) => value + 1);

        public int GetPartitionEofCallbackCount(TopicPartition topicPartition) => PartitionEofCallbacksDictionary[topicPartition];
    }
}
