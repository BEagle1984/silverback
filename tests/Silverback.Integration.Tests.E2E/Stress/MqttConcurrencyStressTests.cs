// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

namespace Silverback.Tests.Integration.E2E.Stress;

/// <summary>
///     Hammers the MQTT consumer pipeline with 1200 messages produced concurrently
///     to maximize callback overlap in <c>OnMessageReceivedAsync</c>. Designed to
///     surface known concurrency bugs under realistic load:
///     <list type="bullet">
///         <item>MC1: _nextChannelIndex non-atomic increment losing message routing</item>
///         <item>MC2: publish queue channel recreation race on reconnect</item>
///     </list>
///     Expected to fail on the current codebase (timeout or assertion failure).
/// </summary>
[Trait("Type", "Stress")]
[Trait("Broker", "MQTT")]
public class MqttConcurrencyStressTests : MqttTests
{
    private const string StressTopicName = "stress/topic";

    private const string StressClientId = "stress-client";

    private const int TotalMessages = 1200;

    public MqttConcurrencyStressTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact(Timeout = 60_000)]
    public async Task RapidProduction_ShouldConsumeAllMessagesWithoutLoss()
    {
        ConcurrentBag<string> receivedContent = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("stress-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(StressClientId)
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(StressTopicName))
                                .Consume(
                                    endpoint => endpoint.ConsumeFrom(StressTopicName))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        // Sync subscriber with a spin to increase the chance of concurrent
        // MQTTnet callbacks overlapping in OnMessageReceivedAsync.
        void HandleMessage(TestEventOne message)
        {
            receivedContent.Add(message.ContentEventOne!);
            Thread.SpinWait(100);
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        // Rapid-fire: produce all messages concurrently to maximize callback overlap
        Task[] produceTasks = Enumerable.Range(0, TotalMessages)
            .Select(i => publisher.PublishEventAsync(
                new TestEventOne { ContentEventOne = $"{i}" }).AsTask())
            .ToArray();

        await Task.WhenAll(produceTasks);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // No message loss
        receivedContent.Count.ShouldBe(
            TotalMessages,
            $"Received {receivedContent.Count}/{TotalMessages} messages. " +
            "Possible causes: MC1 (_nextChannelIndex race routing messages " +
            "to wrong/skipped channels), or thread starvation from sync " +
            "subscriber blocking ThreadPool threads.");

        // No duplicates
        receivedContent.Distinct().Count().ShouldBe(
            TotalMessages,
            "Duplicate messages detected.");

        // All expected indices present
        HashSet<string> expected = [.. Enumerable.Range(0, TotalMessages).Select(i => $"{i}")];
        HashSet<string> received = [.. receivedContent];
        HashSet<string> missing = [.. expected.Except(received)];
        missing.Count.ShouldBe(
            0,
            $"Missing {missing.Count} message(s). First 20: {string.Join(", ", missing.Take(20))}");
    }
}
