// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Extended.Integration.TestHost.Mqtt;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Mqtt;

[Collection(MqttCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "MQTT")]
public class MqttTopicFilterTests
{
    private const int SharedMessagesCount = 20;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(500);

    private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(2);

    private readonly ITestOutputHelper _output;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public MqttTopicFilterTests(MqttFixture fixture, ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public Task Consume_ShouldConsumeParentAndChildLevels_WhenSubscribingMultiLevelWildcard()
    {
        string prefix = "stress-filter-" + Guid.NewGuid().ToString("N");

        // The multi-level wildcard also matches the parent level (MQTT specification 4.7.1.2)
        return VerifyFilterAsync(
            prefix,
            prefix + "/a/#",
            prefix + "/a/probe",
            [prefix + "/a", prefix + "/a/b", prefix + "/a/b/c"],
            [prefix + "/ab", prefix + "/b"]);
    }

    [Fact]
    public Task Consume_ShouldConsumeSingleLevel_WhenSubscribingSingleLevelWildcard()
    {
        string prefix = "stress-filter-" + Guid.NewGuid().ToString("N");

        // The single-level wildcard also matches an empty level
        return VerifyFilterAsync(
            prefix,
            prefix + "/+/x",
            prefix + "/probe/x",
            [prefix + "/1/x", prefix + "//x"],
            [prefix + "/1/2/x", prefix + "/x"]);
    }

    [Fact]
    public async Task Consume_ShouldConsumeEachMessageOnce_WhenClientsShareSubscription()
    {
        string prefix = "stress-filter-" + Guid.NewGuid().ToString("N");
        string topic = prefix + "/shared";
        string filter = $"$share/{prefix}-group/{topic}";
        string[] clientIds = [prefix + "-1", prefix + "-2"];
        FilterMessage[] messages =
        [
            .. Enumerable.Range(0, SharedMessagesCount).Select(number => new FilterMessage { Topic = topic, Number = number })
        ];

        // The shared subscription matches only its topic, so the probes are published to the same topic
        ConsumedMessage[] consumed = await ConsumeAsync(prefix, filter, topic, clientIds, messages, SharedMessagesCount);

        // The distribution depends on the shared subscription strategy configured in the broker
        foreach (string clientId in clientIds)
        {
            _output.WriteLine($"{clientId} consumed {consumed.Count(message => message.ClientId == clientId)} messages");
        }

        consumed.Select(message => message.Message!.Number).Order().ShouldBe(Enumerable.Range(0, SharedMessagesCount));
        consumed.ShouldAllBe(message => message.EndpointTopic == topic && message.Subscription == filter);
    }

    private static IHost CreateHost(
        string filter,
        string[] clientIds,
        ConcurrentQueue<ConsumedMessage> consumed,
        TaskCompletionSource<bool> probeReceived,
        ILoggerProvider logs)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders().AddProvider(logs);
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMqtt())
            .AddMqttClients(clients =>
            {
                clients.ConnectViaTcp(MqttFixture.BrokerHost);

                foreach (string clientId in clientIds)
                {
                    clients.AddClient(client => client
                        .WithClientId(clientId)
                        .Consume<FilterMessage>(endpoint => endpoint
                            .ConsumeFrom(filter)
                            .WithAtLeastOnceQoS()
                            .DeserializeJson(deserializer => deserializer.IgnoreMessageTypeHeader())));
                }
            })
            .AddDelegateSubscriber<IInboundEnvelope<FilterMessage>>(envelope =>
            {
                // The probes only confirm that the subscriptions reached the node the publisher is connected to
                if (envelope.Message is { IsProbe: true })
                    probeReceived.TrySetResult(true);
                else
                    consumed.Enqueue(ConsumedMessage.Create(envelope));
            });

        return builder.Build();
    }

    // Registers the handler before connecting, since the topics are subscribed right after the connection
    private static Task WaitUntilSubscribedAsync(MqttConsumer consumer)
    {
        TaskCompletionSource<bool> subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        consumer.Client.Subscribed.AddHandler(_ =>
        {
            subscribed.TrySetResult(true);

            return ValueTask.CompletedTask;
        });

        return subscribed.Task;
    }

    // The cluster nodes replicate the subscriptions asynchronously, so a message published right after subscribing can be missed
    private static async Task ProbeAsync(IMqttClient publisher, string topic, Task probeReceived)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!probeReceived.IsCompleted)
        {
            stopwatch.Elapsed.ShouldBeLessThan(Timeout, "Timed out waiting for a probe message to be consumed");
            await PublishAsync(publisher, new FilterMessage { Topic = topic, IsProbe = true });
            await Task.WhenAny(probeReceived, Task.Delay(ProbeInterval));
        }
    }

    private static Task PublishAsync(IMqttClient publisher, FilterMessage message) =>
        publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(message.Topic)
            .WithPayload(JsonSerializer.SerializeToUtf8Bytes(message))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build()).WaitAsync(Timeout);

    private static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition())
        {
            stopwatch.Elapsed.ShouldBeLessThan(Timeout, $"Timed out waiting for {description}");
            await Task.Delay(100);
        }
    }

    // Publishes the other topics first and expects only the matching topics, each routed to the endpoint of its actual topic
    private async Task VerifyFilterAsync(
        string prefix,
        string filter,
        string probeTopic,
        string[] matchingTopics,
        string[] otherTopics)
    {
        FilterMessage[] messages = [.. otherTopics.Concat(matchingTopics).Select(topic => new FilterMessage { Topic = topic })];

        ConsumedMessage[] consumed = await ConsumeAsync(prefix, filter, probeTopic, [prefix], messages, matchingTopics.Length);

        consumed.ShouldBe(
            matchingTopics.Select(topic => new ConsumedMessage(prefix, topic, filter, new FilterMessage { Topic = topic })),
            ignoreOrder: true);
    }

    private async Task<ConsumedMessage[]> ConsumeAsync(
        string prefix,
        string filter,
        string probeTopic,
        string[] clientIds,
        FilterMessage[] messages,
        int expectedCount)
    {
        ConcurrentQueue<ConsumedMessage> consumed = new();
        TaskCompletionSource<bool> probeReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CollectingLoggerProvider logs = new();
        using IHost host = CreateHost(filter, clientIds, consumed, probeReceived, logs);

        IBrokerClientsConnector connector = host.Services.GetRequiredService<IBrokerClientsConnector>();
        await connector.InitializeAsync();
        MqttConsumer[] consumers = [.. host.Services.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>()];
        Task[] subscriptions = [.. consumers.Select(WaitUntilSubscribedAsync)];

        using IMqttClient publisher = new MqttClientFactory().CreateMqttClient();

        try
        {
            await connector.ConnectAsync().AsTask().WaitAsync(Timeout);
            await Task.WhenAll(subscriptions).WaitAsync(Timeout);

            // The broker rejects a topic filter through its result code, without failing the subscription
            foreach (MqttConsumer consumer in consumers)
            {
                consumer.Client.SubscribedTopicsFilters.ShouldHaveSingleItem().Topic.ShouldBe(filter);
            }

            await publisher.ConnectAsync(new MqttClientOptionsBuilder()
                .WithTcpServer(MqttFixture.BrokerHost)
                .WithClientId(prefix + "-publisher")
                .Build()).WaitAsync(Timeout);
            await ProbeAsync(publisher, probeTopic, probeReceived.Task);

            foreach (FilterMessage message in messages)
            {
                await PublishAsync(publisher, message);
            }

            await WaitUntilAsync(() => consumed.Count >= expectedCount, "the published messages");

            // Leave time to consume unexpected or duplicate deliveries
            await Task.Delay(SettleTime);

            logs.Errors.ShouldBeEmpty();

            return [.. consumed];
        }
        finally
        {
            try
            {
                await connector.DisconnectAsync().AsTask().WaitAsync(Timeout);
            }
            finally
            {
                _output.WriteLine($"Filter={filter}; consumed messages: {string.Join(", ", consumed)}");

                foreach (string entry in logs.Entries)
                {
                    _output.WriteLine($"Log: {entry}");
                }
            }

            if (publisher.IsConnected)
                await publisher.DisconnectAsync();
        }
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

        public bool IsProbe { get; init; }
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<string> Entries { get; } = new();

        public ConcurrentQueue<string> Errors { get; } = new();

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string entry = $"{logLevel} {eventId.Id}: {formatter(state, exception)}";

            if (exception != null)
                entry += $" | {exception.GetType().Name}: {exception.Message}";

            Entries.Enqueue(entry);

            if (logLevel >= LogLevel.Error)
                Errors.Enqueue(entry);
        }

        public void Dispose()
        {
            // No resources to release
        }
    }
}
