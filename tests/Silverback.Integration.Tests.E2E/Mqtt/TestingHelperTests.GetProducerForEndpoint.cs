// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class TestingHelperTests
{
    private static readonly byte[] AesEncryptionKey = BytesUtil.GetRandomBytes(32);

    [Fact]
    public async Task GetProducerForEndpoint_ShouldEncrypt_WhenConsumerDecryptsUsingAes()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .DecryptUsingAes(AesEncryptionKey))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "encrypted" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem().RawMessage.ShouldBeOfType<SymmetricEncryptStream>();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("encrypted");
    }

    [Theory]
    [InlineData("e2e/topic1")]
    [InlineData("e2e/topic2")]
    public async Task GetProducerForEndpoint_ShouldProduceToTopic_WhenEndpointSubscribesMultipleTopics(string topic)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint.ConsumeFrom("e2e/topic1", "e2e/topic2"))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(topic).ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe(topic);
    }

    [Theory]
    [InlineData("multi")]
    [InlineData("e2e/topic1,e2e/topic2")]
    public async Task GetProducerForEndpoint_ShouldProduceToFirstTopic_WhenMultiTopicEndpointIsResolvedByName(string endpointName)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume("multi", endpoint => endpoint.ConsumeFrom("e2e/topic1", "e2e/topic2"))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(endpointName).ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe("e2e/topic1");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToFirstConcreteTopic_WhenNamedEndpointSubscribesWildcardFilterFirst()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume("mixed", endpoint => endpoint.ConsumeFrom("e2e/+/wildcard", "e2e/concrete"))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint("mixed").ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe("e2e/concrete");
    }

    [Theory]
    [InlineData("e2e/sensors/+/readings", "e2e/sensors/1/readings")]
    [InlineData("e2e/sensors/#", "e2e/sensors/1/readings")]
    [InlineData("$share/group/e2e/sensors/+/readings", "e2e/sensors/1/readings")]
    public async Task GetProducerForEndpoint_ShouldProduceToTopicMatchingWildcardFilter(string topicFilter, string topic)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>("sensors", endpoint => endpoint.ConsumeFrom(topicFilter))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(topic).ProduceAsync(new TestEventOne { ContentEventOne = "reading" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInboundEnvelope envelope = Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        envelope.Endpoint.RawName.ShouldBe(topic);
        envelope.Endpoint.Configuration.FriendlyName.ShouldBe("sensors");
        envelope.Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("reading");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldPreferEndpointSubscribingTopic_WhenTopicAlsoMatchesWildcardFilter()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("client1")
                    .Consume("wildcard", endpoint => endpoint.ConsumeFrom("e2e/+").ConsumeRaw()))
                .AddClient(client => client
                    .WithClientId("client2")
                    .Consume("name", endpoint => endpoint.ConsumeFrom("e2e/a", "e2e/b").ConsumeStrings()))
                .AddClient(client => client
                    .WithClientId("client3")
                    .Consume("other-wildcard", endpoint => endpoint.ConsumeFrom("e2e/#").ConsumeRaw())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint("e2e/b").ProduceAsync(new StringMessage("hello"));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // All endpoints receive the message, but it is serialized for the endpoint subscribing the exact topic
        IOutboundEnvelope outboundEnvelope = Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem();
        outboundEnvelope.EndpointConfiguration.RawName.ShouldBe("e2e/b");
        outboundEnvelope.EndpointConfiguration.Serializer.ShouldBeOfType<StringMessageSerializer>();
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("wildcards")]
    [InlineData("e2e/+/a,e2e/#")]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenEndpointOnlySubscribesWildcardFilters(string endpointName)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume("wildcards", endpoint => endpoint.ConsumeFrom("e2e/+/a", "e2e/#"))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint(endpointName);

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldStartWith(
            "Cannot produce to the endpoint 'wildcards (e2e/+/a,e2e/#)', since it only subscribes wildcard topic filters.");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenFriendlyNameDoesNotMatchOwnWildcardFiltersButMatchesOtherEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("client1")
                    .Consume("e2e/sensors", endpoint => endpoint.ConsumeFrom("e2e/sensors/+")))
                .AddClient(client => client
                    .WithClientId("client2")
                    .Consume("all", endpoint => endpoint.ConsumeFrom("#").ConsumeRaw())))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint("e2e/sensors");

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldStartWith(
            "Cannot produce to the endpoint 'e2e/sensors (e2e/sensors/+)', since it only subscribes wildcard topic filters.");
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldProduceToFriendlyName_WhenFriendlyNameMatchesOwnWildcardFilter()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId("client1")
                    .Consume("e2e", endpoint => endpoint.ConsumeFrom("e2e/#").ConsumeStrings()))
                .AddClient(client => client
                    .WithClientId("client2")
                    .Consume("all", endpoint => endpoint.ConsumeFrom("#").ConsumeRaw())))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint("e2e").ProduceAsync(new StringMessage("hello"));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The named endpoint is mirrored, even if another endpoint's wildcard filter matches the topic too
        IOutboundEnvelope outboundEnvelope = Helper.Spy.OutboundEnvelopes.ShouldHaveSingleItem();
        outboundEnvelope.EndpointConfiguration.RawName.ShouldBe("e2e");
        outboundEnvelope.EndpointConfiguration.Serializer.ShouldBeOfType<StringMessageSerializer>();
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("e2e/+/a")]
    [InlineData("e2e/#")]
    public async Task GetProducerForEndpoint_ShouldThrow_WhenEndpointNameIsWildcardFilter(string endpointName)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint.ConsumeFrom("e2e/+/a", "e2e/#"))))
            .AddIntegrationSpyAndSubscriber());

        Action act = () => Helper.GetProducerForEndpoint(endpointName);

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldStartWith($"Cannot produce to '{endpointName}', since it is a wildcard topic filter.");
    }

    [Theory]
    [InlineData("$share/group/e2e/shared")]
    [InlineData("e2e/shared")]
    [InlineData("shared")]
    public async Task GetProducerForEndpoint_ShouldProduceToTopic_WhenEndpointUsesSharedSubscription(string endpointName)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume("shared", endpoint => endpoint.ConsumeFrom("$share/group/e2e/shared"))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(endpointName).ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.GetMessages("e2e/shared").Count.ShouldBe(1);
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Endpoint.RawName.ShouldBe("e2e/shared");
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.ExactlyOnce)]
    public async Task GetProducerForEndpoint_ShouldMirrorQualityOfServiceLevel(MqttQualityOfServiceLevel qosLevel)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithQualityOfServiceLevel(qosLevel))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        GetDefaultTopicMessages().ShouldHaveSingleItem().QualityOfServiceLevel.ShouldBe(qosLevel);
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task GetProducerForEndpoint_ShouldMirrorProtocolVersion()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .UseProtocolVersion(MqttProtocolVersion.V311)
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne { ContentEventOne = "v3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The user properties (headers) are not supported by MQTT prior to version 5
        GetDefaultTopicMessages().ShouldHaveSingleItem().UserProperties.ShouldBeNull();
        Helper.Spy.InboundEnvelopes.ShouldHaveSingleItem().Message.ShouldBeOfType<TestEventOne>().ContentEventOne.ShouldBe("v3");
    }
}
