// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.KafkaAndMqtt;

[Trait("Broker", "Kafka+MQTT")]
public class MqttToKafkaFixture : KafkaFixture
{
    public MqttToKafkaFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboundAndInbound_MqttToKafka_ProducedAndConsumed()
    {
        int eventOneCount = 0;
        int eventTwoCount = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt().AddMockedKafka())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId("client1")
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("some/topic"))
                                .Consume(endpoint => endpoint.ConsumeFrom("some/topic"))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("some-topic")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("some-topic"))))
                .AddDelegateSubscriber<TestEventOne, TestEventTwo>(HandleEventOne)
                .AddDelegateSubscriber<TestEventTwo>(HandleEventTwo));

        TestEventTwo HandleEventOne(TestEventOne eventOne)
        {
            Interlocked.Increment(ref eventOneCount);
            return new TestEventTwo { ContentEventTwo = eventOne.ContentEventOne };
        }

        void HandleEventTwo(TestEventTwo message) => Interlocked.Increment(ref eventTwoCount);

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        IMqttTestingHelper mqttTestingHelper = Host.ServiceProvider.GetRequiredService<IMqttTestingHelper>();

        await mqttTestingHelper.WaitUntilConnectedAsync();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => eventOneCount >= 15);
        await AsyncTestingUtil.WaitAsync(() => eventTwoCount >= 15);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        eventOneCount.Should().Be(15);
        eventTwoCount.Should().Be(15);
        DefaultConsumerGroup.GetCommittedOffsetsCount("some-topic").Should().Be(15);
    }
}
