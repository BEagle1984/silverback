// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class RequestResponseFixture : MqttFixture
{
    public RequestResponseFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Producer_ShouldSetResponseTopicAndCorrelationData()
    {
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
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.SetResponseTopic("response/one").SetCorrelationData("data"));
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.SetResponseTopic("response/two").SetCorrelationData([1, 2, 3, 4]));

        IReadOnlyList<MqttApplicationMessage> messages = GetDefaultTopicMessages();
        messages.Should().HaveCount(2);
        messages[0].ResponseTopic.Should().Be("response/one");
        messages[0].CorrelationData.Should().BeEquivalentTo("data"u8.ToArray());
        messages[0].UserProperties.Should().NotContain(property => property.Name == MqttMessageHeaders.ResponseTopic);
        messages[0].UserProperties.Should().NotContain(property => property.Name == MqttMessageHeaders.CorrelationData);
        messages[1].ResponseTopic.Should().Be("response/two");
        messages[1].CorrelationData.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4 });
        messages[1].UserProperties.Should().NotContain(property => property.Name == MqttMessageHeaders.ResponseTopic);
        messages[1].UserProperties.Should().NotContain(property => property.Name == MqttMessageHeaders.CorrelationData);
    }

    [Fact]
    public async Task Consumer_ShouldPropagateResponseTopicAndCorrelationData()
    {
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
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.SetResponseTopic("response/one").SetCorrelationData("data"));
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.SetResponseTopic("response/two").SetCorrelationData([1, 2, 3, 4]));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].GetResponseTopic().Should().Be("response/one");
        Helper.Spy.InboundEnvelopes[0].GetCorrelationData().Should().BeEquivalentTo("data"u8.ToArray());
        Helper.Spy.InboundEnvelopes[1].GetResponseTopic().Should().Be("response/two");
        Helper.Spy.InboundEnvelopes[1].GetCorrelationData().Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4 });
    }
}
