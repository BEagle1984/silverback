// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class InboundMessageFilterTests : MqttTests
{
    public InboundMessageFilterTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Filter_ShouldSkipAndAcknowledgeFilteredMessages()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .Filter(message => message?.ContentEventOne != "discard"))))
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedContents.ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task Filter_ShouldNotStopConsumer_WhenFilteredMessageHasNoSubscriber()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .ThrowIfUnhandled()
                        .Filter(message => message is not TestEventTwo))))
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "unhandled" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "unhandled" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedContents.ShouldBe(["1", "2"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task Filter_ShouldSkipAndAcknowledgeFilteredMessagesInBatch()
    {
        TestingCollection<List<string?>> receivedBatches = [];
        int evaluatedCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .Filter(
                            message =>
                            {
                                Interlocked.Increment(ref evaluatedCount);
                                return message?.ContentEventOne != "discard";
                            }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });

        // The filtered messages are acknowledged while the batch is pending, which still holds the first message
        await AsyncTestingUtil.WaitAsync(() => evaluatedCount == 3 && DefaultClientSession.GetPendingMessagesCount() == 1);
        evaluatedCount.ShouldBe(3);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "4" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "5" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "6" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].ShouldBe(["1", "2", "3"]);
        receivedBatches[1].ShouldBe(["4", "5", "6"]);
        evaluatedCount.ShouldBe(11);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
        Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single().StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
    }

    [Fact]
    public async Task Filter_ShouldSkipAndAcknowledgeFilteredChunkedMessages()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .EnableChunking(10))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .Filter(message => message?.ContentEventOne != "Long message to discard"))))
            .AddIntegrationSpy()
            .AddDelegateSubscriber<TestEventOne>(message => receivedContents.Add(message.ContentEventOne)));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message to discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message to discard" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedContents.ShouldBe(["Long message 1"]);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["Long message 1"]);
        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBeGreaterThan(6);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }

    [Fact]
    public async Task Filter_ShouldSkipAndAcknowledgeFilteredMessages_WhenStreaming()
    {
        TestingCollection<string?> receivedContents = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming()
                        .Filter(message => message?.ContentEventOne != "discard"))))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamEnumerable<TestEventOne> stream)
        {
            foreach (TestEventOne message in stream)
            {
                receivedContents.Add(message.ContentEventOne);
            }
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "discard" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedContents.ShouldBe(["1", "2", "3"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
    }
}
