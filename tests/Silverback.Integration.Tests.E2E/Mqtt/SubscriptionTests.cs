// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class SubscriptionTests : MqttTestFixture
{
    public SubscriptionTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Subscription_SingleLevelWildcard_MessagesConsumed()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                config => config
                                    .WithClientId("e2e-test")
                                    .ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo("world/news"))
                            .AddOutbound<TestEventTwo>(endpoint => endpoint.ProduceTo("world/europe/news"))
                            .AddOutbound<TestEventThree>(endpoint => endpoint.ProduceTo("world/europe/switzerland/news"))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom("world/+/news")))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventTwo>();
    }

    [Fact]
    public async Task Subscription_MultiLevelWildcard_MessagesConsumed()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                config => config
                                    .WithClientId("e2e-test")
                                    .ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo("world/news"))
                            .AddOutbound<TestEventTwo>(endpoint => endpoint.ProduceTo("world/europe/news"))
                            .AddOutbound<TestEventThree>(endpoint => endpoint.ProduceTo("world/europe/switzerland/news"))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom("world/#/news")))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventThree>();
    }

    [Fact]
    public async Task Subscription_Shared_MessagesConsumedOnce()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                config => config
                                    .WithClientId("e2e-test")
                                    .ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(config => config.WithClientId("consumer-1"))
                                    .ConsumeFrom("$share/group/" + DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(config => config.WithClientId("consumer-2"))
                                    .ConsumeFrom("$share/group/" + DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = $"{i}"
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }
}
