// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.KafkaAndMQTT
{
    public class MqttToKafkaBasicTests : KafkaTestFixture
    {
        public MqttToKafkaBasicTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task OutboundAndInbound_MqttToKafka_ProducedAndConsumed()
        {
            int eventOneCount = 0;
            int eventTwoCount = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedMqtt()
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
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddDelegateSubscriber(
                            (TestEventOne eventOne) =>
                            {
                                eventOneCount++;
                                return new TestEventTwo { Content = eventOne.Content };
                            })
                        .AddDelegateSubscriber((TestEventTwo _) => eventTwoCount++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await AsyncTestingUtil.WaitAsync(() => eventOneCount >= 15);
            await AsyncTestingUtil.WaitAsync(() => eventTwoCount >= 15);
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            eventOneCount.Should().Be(15);
            eventTwoCount.Should().Be(15);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
        }
    }
}
