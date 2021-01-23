// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
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
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            envelope =>
                                            {
                                                var testEventOne = (TestEventOne)envelope.Message!;
                                                switch (testEventOne.Content)
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

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            Helper.GetMessagesCount("topic1").Should().Be(1);
            Helper.GetMessagesCount("topic2").Should().Be(1);
            Helper.GetMessagesCount("topic3").Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            Helper.GetMessagesCount("topic1").Should().Be(2);
            Helper.GetMessagesCount("topic2").Should().Be(1);
            Helper.GetMessagesCount("topic3").Should().Be(2);
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
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            "topic{0}",
                                            envelope =>
                                            {
                                                var testEventOne = (TestEventOne)envelope.Message!;
                                                switch (testEventOne.Content)
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

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            Helper.GetMessagesCount("topic1").Should().Be(1);
            Helper.GetMessagesCount("topic2").Should().Be(1);
            Helper.GetMessagesCount("topic3").Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            Helper.GetMessagesCount("topic1").Should().Be(2);
            Helper.GetMessagesCount("topic2").Should().Be(1);
            Helper.GetMessagesCount("topic3").Should().Be(2);
        }

        [Fact]
        public async Task DynamicRouting_CustomNameResolver_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSingleton<TestEndpointNameResolver>()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .UseEndpointNameResolver<TestEndpointNameResolver>())))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            Helper.GetMessagesCount("topic1").Should().Be(1);
            Helper.GetMessagesCount("topic2").Should().Be(1);
            Helper.GetMessagesCount("topic3").Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            Helper.GetMessagesCount("topic1").Should().Be(2);
            Helper.GetMessagesCount("topic2").Should().Be(1);
            Helper.GetMessagesCount("topic3").Should().Be(2);
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class TestEndpointNameResolver : IProducerEndpointNameResolver
        {
            public string GetName(IOutboundEnvelope envelope)
            {
                var testEventOne = (TestEventOne)envelope.Message!;
                switch (testEventOne.Content)
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
            }
        }
    }
}
