// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class MqttTopicFilterTests : BrokerHarnessTests<MqttTestHarness>
{
    private const int SharedMessagesCount = 20;

    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(2);

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public MqttTopicFilterTests(MqttFixture fixture, ITestOutputHelper output)
        : base(new MqttTestHarness("filter", output))
    {
    }

    [Fact]
    public Task Consume_ShouldConsumeParentAndChildLevels_WhenSubscribingMultiLevelWildcard()
    {
        string prefix = Harness.Prefix;

        // The multi-level wildcard also matches the parent level (MQTT specification 4.7.1.2)
        return VerifyFilterAsync(
            prefix + "/a/#",
            [prefix + "/a", prefix + "/a/b", prefix + "/a/b/c"],
            [prefix + "/ab", prefix + "/b"]);
    }

    [Fact]
    public Task Consume_ShouldConsumeSingleLevel_WhenSubscribingSingleLevelWildcard()
    {
        string prefix = Harness.Prefix;

        // The single-level wildcard also matches an empty level
        return VerifyFilterAsync(
            prefix + "/+/x",
            [prefix + "/1/x", prefix + "//x"],
            [prefix + "/1/2/x", prefix + "/x"]);
    }

    [Fact]
    public async Task Consume_ShouldConsumeEachMessageOnce_WhenClientsShareSubscription()
    {
        string prefix = Harness.Prefix;
        string topic = prefix + "/shared";
        string filter = $"$share/{prefix}-group/{topic}";
        string[] clientIds = [prefix + "-1", prefix + "-2"];
        FilterMessage[] messages =
        [
            .. Enumerable.Range(0, SharedMessagesCount).Select(number => new FilterMessage { Topic = topic, Number = number })
        ];

        ConsumedMessage[] consumed = await ConsumeAsync(filter, clientIds, messages, SharedMessagesCount);

        // The distribution depends on the shared subscription strategy configured in the broker
        foreach (string clientId in clientIds)
        {
            Harness.Output.WriteLine($"{clientId} consumed {consumed.Count(message => message.ClientId == clientId)} messages");
        }

        consumed.Select(message => message.Message!.Number).Order().ShouldBe(Enumerable.Range(0, SharedMessagesCount));
        consumed.ShouldAllBe(message => message.EndpointTopic == topic && message.Subscription == filter);
    }

    // Publishes the other topics first and expects only the matching topics, each routed to the endpoint of its actual topic
    private async Task VerifyFilterAsync(
        string filter,
        string[] matchingTopics,
        string[] otherTopics)
    {
        FilterMessage[] messages = [.. otherTopics.Concat(matchingTopics).Select(topic => new FilterMessage { Topic = topic })];

        ConsumedMessage[] consumed = await ConsumeAsync(filter, [Harness.Prefix], messages, matchingTopics.Length);

        consumed.ShouldBe(
            matchingTopics.Select(topic => new ConsumedMessage(Harness.Prefix, topic, filter, new FilterMessage { Topic = topic })),
            ignoreOrder: true);
    }

    private async Task<ConsumedMessage[]> ConsumeAsync(
        string filter,
        string[] clientIds,
        FilterMessage[] messages,
        int expectedCount)
    {
        ConcurrentQueue<ConsumedMessage> consumed = new();

        foreach (string clientId in clientIds)
        {
            Harness.AddClient(clientId, client => client
                .Consume<FilterMessage>(endpoint => endpoint
                    .ConsumeFrom(filter)
                    .WithAtLeastOnceQoS()
                    .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())));
        }

        Harness.ConfigureSilverback(silverback => silverback
            .AddDelegateSubscriber<IInboundEnvelope<FilterMessage>>(envelope => consumed.Enqueue(ConsumedMessage.Create(envelope))));
        Harness.OnDispose(() => Harness.Output.WriteLine($"Filter={filter}; consumed messages: {string.Join(", ", consumed)}"));

        await Harness.InitializeAsync();
        await Harness.ConnectAsync();

        // The broker rejects a topic filter through its result code, without failing the subscription
        foreach (MqttConsumer consumer in Harness.Consumers)
        {
            consumer.Client.SubscribedTopicsFilters.ShouldHaveSingleItem().Topic.ShouldBe(filter);
        }

        TestMqttClient publisher = await Harness.CreateClientAsync("publisher");

        foreach (FilterMessage message in messages)
        {
            await publisher.PublishJsonAsync(message.Topic, message, MqttQualityOfServiceLevel.AtLeastOnce);
        }

        await Harness.WaitUntilAsync(() => consumed.Count >= expectedCount, "the published messages");

        // Leave time to consume unexpected or duplicate deliveries
        await Task.Delay(SettleTime);

        Harness.Logs.Errors.ShouldBeEmpty();

        return [.. consumed];
    }

    // The consuming client, the topic of the envelope's endpoint, the topic filter subscribed by its configuration and the message,
    // which carries the topic it was published to
    private sealed record ConsumedMessage(string ClientId, string EndpointTopic, string Subscription, FilterMessage? Message)
    {
        public static ConsumedMessage Create(IInboundEnvelope<FilterMessage> envelope) =>
            new(
                ((MqttMessageIdentifier)envelope.BrokerMessageIdentifier).ClientId,
                envelope.Endpoint.RawName,
                envelope.Endpoint.Configuration.RawName,
                envelope.Message);
    }

    private sealed record FilterMessage
    {
        public string Topic { get; init; } = string.Empty;

        public int Number { get; init; }
    }
}
