// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class OutboundMessageFilterTests : MqttTests
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(10);

    public OutboundMessageFilterTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessages_WhenPublishingEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .Filter(eventOne => eventOne?.ContentEventOne != "discard"))))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
                new TestEventOne[]
                {
                    new() { ContentEventOne = "one" },
                    new() { ContentEventOne = "discard" },
                    new() { ContentEventOne = "two" },
                    new() { ContentEventOne = "discard" }
                }.AsEnumerable())
            .WaitAsync(PublishTimeout);

        Helper.Spy.RawOutboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["one", "two"]);
        GetDefaultTopicMessages().Count.ShouldBe(2);
    }

    [Fact]
    public async Task Filter_ShouldFilterOutboundMessages_WhenPublishingAsyncEnumerable()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .Filter(eventOne => eventOne?.ContentEventOne != "discard"))))
            .AddIntegrationSpy());

        static async IAsyncEnumerable<TestEventOne> GetMessagesAsync()
        {
            yield return new TestEventOne { ContentEventOne = "discard" };
            await Task.Delay(10);
            yield return new TestEventOne { ContentEventOne = "one" };
            await Task.Delay(10);
            yield return new TestEventOne { ContentEventOne = "discard" };
            yield return new TestEventOne { ContentEventOne = "two" };
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(GetMessagesAsync()).WaitAsync(PublishTimeout);

        Helper.Spy.RawOutboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["one", "two"]);
        GetDefaultTopicMessages().Count.ShouldBe(2);
    }
}
