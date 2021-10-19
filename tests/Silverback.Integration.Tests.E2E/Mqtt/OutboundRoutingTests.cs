// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class OutboundRoutingTests : MqttTestFixture
{
    public OutboundRoutingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task DynamicRouting_NameFunction_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        message =>
                                        {
                                            switch (message?.Content)
                                            {
                                                case "1":
                                                    return "topic1";
                                                case "2":
                                                    return "topic2";
                                                case "3":
                                                    return "topic3";
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        }))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(1);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(2);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(2);
    }

    [Fact]
    public async Task DynamicRouting_NameFormat_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        "topic{0}",
                                        message =>
                                        {
                                            switch (message?.Content)
                                            {
                                                case "1":
                                                    return new[] { "1" };
                                                case "2":
                                                    return new[] { "2" };
                                                case "3":
                                                    return new[] { "3" };
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        }))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(1);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(2);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(2);
    }

    [Fact]
    public async Task DynamicRouting_CustomNameResolver_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSingleton<TestEndpointResolver>()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                    .AddMqttEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .UseEndpointResolver<TestEndpointResolver>())))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(1);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        Helper.GetMessages("topic1").Should().HaveCount(2);
        Helper.GetMessages("topic2").Should().HaveCount(1);
        Helper.GetMessages("topic3").Should().HaveCount(2);
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(TestEventOne? message) =>
            message?.Content switch
            {
                "1" => "topic1",
                "2" => "topic2",
                "3" => "topic3",
                _ => throw new InvalidOperationException()
            };
    }
}
