// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
                    .UseModel()
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
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StatusInfo_ShouldRecordHistory_WhenConsumingAndDisconnecting()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
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

        consumer.StatusInfo.History.Should().HaveCount(2);
        consumer.StatusInfo.History.First().Status.Should().Be(ConsumerStatus.Started);
        consumer.StatusInfo.History.First().Timestamp.Should().NotBeNull();
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Connected);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.StatusInfo.History.Should().HaveCount(3);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Consuming);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

        await consumer.Client.DisconnectAsync();

        consumer.StatusInfo.History.Should().HaveCount(4);
        consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Stopped);
        consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
    }

    [Fact]
    public async Task StatusInfo_ShouldTrackLatestConsumedMessage()
    {
        await Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
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
        consumer.StatusInfo.LatestConsumedMessageIdentifier.Should().BeOfType<MqttMessageIdentifier>();
        consumer.StatusInfo.LatestConsumedMessageIdentifier.As<MqttMessageIdentifier>().ClientId.Should().Be(DefaultClientId);
        consumer.StatusInfo.LatestConsumedMessageTimestamp.Should().NotBeNull();
    }
}
