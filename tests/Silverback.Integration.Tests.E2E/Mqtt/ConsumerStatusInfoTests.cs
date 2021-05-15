// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class ConsumerStatusInfoTests : MqttTestFixture
    {
        public ConsumerStatusInfoTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task StatusInfo_ConsumingAndDisconnecting_StatusIsCorrectlySet()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedMqtt(
                                mockedKafkaOptions =>
                                    mockedKafkaOptions.DelayConnection(TimeSpan.FromMilliseconds(200))))
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run(waitUntilBrokerConnected: false);

            var consumer = Helper.Broker.Consumers[0];

            consumer.IsConnected.Should().BeTrue();
            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Connected);

            await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Ready);
            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Ready);

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            consumer.IsConnected.Should().BeTrue();
            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);

            await Helper.Broker.DisconnectAsync();

            consumer.IsConnected.Should().BeFalse();
            consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Disconnected);
        }

        [Fact]
        public async Task StatusInfo_ConsumingAndDisconnecting_StatusHistoryRecorded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedMqtt(
                                mockedKafkaOptions =>
                                    mockedKafkaOptions.DelayConnection(TimeSpan.FromMilliseconds(200))))
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run(waitUntilBrokerConnected: false);

            var consumer = Helper.Broker.Consumers[0];

            consumer.StatusInfo.History.Should().HaveCount(1);
            consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Connected);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

            await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.History.Count >= 2);

            consumer.StatusInfo.History.Should().HaveCount(2);
            consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Ready);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            consumer.StatusInfo.History.Should().HaveCount(3);
            consumer.StatusInfo.History.Last().Status.Should().Be(ConsumerStatus.Consuming);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();

            await Helper.Broker.DisconnectAsync();

            consumer.StatusInfo.History.Should().HaveCount(4);
            consumer.StatusInfo.History.Last().Status.Should()
                .Be(ConsumerStatus.Disconnected);
            consumer.StatusInfo.History.Last().Timestamp.Should().NotBeNull();
        }

        [Fact]
        public async Task StatusInfo_Consuming_LatestConsumedMessageTracked()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedMqtt(
                                mockedKafkaOptions =>
                                    mockedKafkaOptions.DelayConnection(TimeSpan.FromMilliseconds(100))))
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            await Helper.WaitUntilConnectedAsync();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Broker.Consumers[0].StatusInfo.LatestConsumedMessageTimestamp.Should().NotBeNull();
        }
    }
}
