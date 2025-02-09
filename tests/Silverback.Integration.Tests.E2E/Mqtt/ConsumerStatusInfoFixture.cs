// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class ConsumerStatusInfoFixture : MqttFixture
{
    public ConsumerStatusInfoFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task StatusInfo_ShouldReportCorrectStatus_WhenConsumingAndDisconnecting()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttClients(
                        clients => clients
                            .ConnectViaTcp("e2e-mqtt-broker")
                            .AddClient(
                                consumer => consumer
                                    .WithClientId(DefaultClientId)
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                    .AddIntegrationSpyAndSubscriber())
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Connected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Connected);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Consuming);

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StatusInfo_ShouldRecordHistory_WhenConsumingAndDisconnecting()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttClients(
                        clients => clients
                            .ConnectViaTcp("e2e-mqtt-broker")
                            .AddClient(
                                consumer => consumer
                                    .WithClientId(DefaultClientId)
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                    .AddIntegrationSpyAndSubscriber())
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.History.Count >= 2);

        consumer.StatusInfo.History.Count.ShouldBe(2);
        consumer.StatusInfo.History.First().Status.ShouldBe(ConsumerStatus.Started);
        consumer.StatusInfo.History.First().Timestamp.ShouldNotBeNull();
        consumer.StatusInfo.History.Last().Status.ShouldBe(ConsumerStatus.Connected);
        consumer.StatusInfo.History.Last().Timestamp.ShouldNotBeNull();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.History.Count.ShouldBe(3);
        consumer.StatusInfo.History.Last().Status.ShouldBe(ConsumerStatus.Consuming);
        consumer.StatusInfo.History.Last().Timestamp.ShouldNotBeNull();

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.History.Count.ShouldBe(4);
        consumer.StatusInfo.History.Last().Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.StatusInfo.History.Last().Timestamp.ShouldNotBeNull();
    }

    [Fact]
    public async Task StatusInfo_ShouldTrackLatestConsumedMessage()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttClients(
                        clients => clients
                            .ConnectViaTcp("e2e-mqtt-broker")
                            .AddClient(
                                consumer => consumer
                                    .WithClientId(DefaultClientId)
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                    .AddIntegrationSpyAndSubscriber())
            .RunAsync(waitUntilBrokerClientsConnected: false);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.LatestConsumedMessageIdentifier.ShouldBeOfType<MqttMessageIdentifier>();
        consumer.StatusInfo.LatestConsumedMessageIdentifier.ShouldBeOfType<MqttMessageIdentifier>().ClientId.ShouldBe(DefaultClientId);
        consumer.StatusInfo.LatestConsumedMessageTimestamp.ShouldNotBeNull();
    }
}
