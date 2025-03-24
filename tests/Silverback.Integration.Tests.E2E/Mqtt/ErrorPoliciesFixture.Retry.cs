// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task RetryPolicy_ShouldRetryProcessingMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        tryCount.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));
    }

    [Fact]
    public async Task RetryPolicy_ShouldCommit_WhenSuccessfulAfterSomeRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(3);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task RetryPolicy_ShouldNotCommit_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .RequestPersistentSession()
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryEncryptedMessageMultipleTimes()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        Stream rawMessage = await DefaultSerializers.Json.SerializeAsync(message);
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .EncryptUsingAes(AesEncryptionKey))
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DecryptUsingAes(AesEncryptionKey)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            if (tryCount != 3)
                throw new InvalidOperationException("Retry!");
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReadAll().ShouldNotBe(rawMessage.ReReadAll());
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task RetryPolicy_ShouldStopConsumer_WhenStillFailingAfterRetries()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.Retry(10)))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent message)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(11);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }
}
