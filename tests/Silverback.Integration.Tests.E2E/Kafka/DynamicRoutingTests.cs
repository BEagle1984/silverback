// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class DynamicRoutingTests : E2ETestFixture
    {
        public DynamicRoutingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task DynamicRouting_CustomOutboundRouter_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddSingletonOutboundRouter<TestOutboundRouter>()
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .AddOutbound<TestEventOne, TestOutboundRouter>())
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var topic1 = GetTopic("topic1");
            var topic2 = GetTopic("topic2");
            var topic3 = GetTopic("topic3");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "1" });
            await publisher.PublishAsync(new TestEventOne { Content = "2" });
            await publisher.PublishAsync(new TestEventOne { Content = "3" });

            topic1.TotalMessagesCount.Should().Be(1);
            topic2.TotalMessagesCount.Should().Be(1);
            topic3.TotalMessagesCount.Should().Be(1);

            await publisher.PublishAsync(new TestEventOne { Content = "1,2,3" });
            await publisher.PublishAsync(new TestEventOne { Content = "2,1" });

            topic1.TotalMessagesCount.Should().Be(3);
            topic2.TotalMessagesCount.Should().Be(3);
            topic3.TotalMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task DynamicRouting_GenericSingleEndpointRouter_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddSingletonOutboundRouter<TestOutboundRouter>()
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary["one"],
                                        "two" => endpointsDictionary["two"],
                                        _ => endpointsDictionary["three"]
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1") },
                                        { "two", endpoint => endpoint.ProduceTo("topic2") },
                                        { "three", endpoint => endpoint.ProduceTo("topic3") }
                                    })
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) => ((TestEventTwo)message).Content switch
                                    {
                                        "one" => endpointsDictionary["one"],
                                        "two" => endpointsDictionary["two"],
                                        _ => endpointsDictionary["three"]
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA") },
                                        { "two", endpoint => endpoint.ProduceTo("topicB") },
                                        { "three", endpoint => endpoint.ProduceTo("topicC") }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var topic1 = GetTopic("topic1");
            var topic2 = GetTopic("topic2");
            var topic3 = GetTopic("topic3");
            var topicA = GetTopic("topicA");
            var topicB = GetTopic("topicB");
            var topicC = GetTopic("topicC");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two" });

            topic1.TotalMessagesCount.Should().Be(1);
            topic2.TotalMessagesCount.Should().Be(1);
            topic3.TotalMessagesCount.Should().Be(0);
            topicA.TotalMessagesCount.Should().Be(0);
            topicB.TotalMessagesCount.Should().Be(0);
            topicC.TotalMessagesCount.Should().Be(0);

            await publisher.PublishAsync(new TestEventTwo { Content = "one" });
            await publisher.PublishAsync(new TestEventTwo { Content = "two" });

            topic1.TotalMessagesCount.Should().Be(1);
            topic2.TotalMessagesCount.Should().Be(1);
            topic3.TotalMessagesCount.Should().Be(0);
            topicA.TotalMessagesCount.Should().Be(1);
            topicB.TotalMessagesCount.Should().Be(1);
            topicC.TotalMessagesCount.Should().Be(0);
        }

        [Fact]
        public async Task DynamicRouting_GenericMultipleEndpointRouter_MessagesRouted()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddSingletonOutboundRouter<TestOutboundRouter>()
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://tests"; })
                                .AddOutbound<TestEventOne>(
                                    (message, _, endpointsDictionary) => message.Content switch
                                    {
                                        "one" => endpointsDictionary.Values.Take(1),
                                        "two-three" => endpointsDictionary.Values.Skip(1),
                                        _ => throw new ArgumentOutOfRangeException()
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topic1") },
                                        { "two", endpoint => endpoint.ProduceTo("topic2") },
                                        { "three", endpoint => endpoint.ProduceTo("topic3") }
                                    })
                                .AddOutbound(
                                    typeof(TestEventTwo),
                                    (message, _, endpointsDictionary) => ((TestEventTwo)message).Content switch
                                    {
                                        "one" => endpointsDictionary.Values.Take(1),
                                        "two-three" => endpointsDictionary.Values.Skip(1),
                                        _ => throw new ArgumentOutOfRangeException()
                                    },
                                    new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                                    {
                                        { "one", endpoint => endpoint.ProduceTo("topicA") },
                                        { "two", endpoint => endpoint.ProduceTo("topicB") },
                                        { "three", endpoint => endpoint.ProduceTo("topicC") }
                                    }))
                        .AddSingletonBrokerBehavior<SpyBrokerBehavior>()
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var topic1 = GetTopic("topic1");
            var topic2 = GetTopic("topic2");
            var topic3 = GetTopic("topic3");
            var topicA = GetTopic("topicA");
            var topicB = GetTopic("topicB");
            var topicC = GetTopic("topicC");
            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two-three" });

            topic1.TotalMessagesCount.Should().Be(1);
            topic2.TotalMessagesCount.Should().Be(1);
            topic3.TotalMessagesCount.Should().Be(1);
            topicA.TotalMessagesCount.Should().Be(0);
            topicB.TotalMessagesCount.Should().Be(0);
            topicC.TotalMessagesCount.Should().Be(0);

            await publisher.PublishAsync(new TestEventTwo { Content = "one" });
            await publisher.PublishAsync(new TestEventTwo { Content = "two-three" });

            topic1.TotalMessagesCount.Should().Be(1);
            topic2.TotalMessagesCount.Should().Be(1);
            topic3.TotalMessagesCount.Should().Be(1);
            topicA.TotalMessagesCount.Should().Be(1);
            topicB.TotalMessagesCount.Should().Be(1);
            topicC.TotalMessagesCount.Should().Be(1);
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class TestOutboundRouter : OutboundRouter<TestEventOne>
        {
            private readonly IReadOnlyList<KafkaProducerEndpoint> _endpoints = new[]
            {
                new KafkaProducerEndpoint("topic1")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    }
                },
                new KafkaProducerEndpoint("topic2")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    }
                },
                new KafkaProducerEndpoint("topic3")
                {
                    Configuration = new KafkaProducerConfig
                    {
                        BootstrapServers = "PLAINTEXT://tests"
                    }
                }
            };

            public override IEnumerable<IProducerEndpoint> Endpoints => _endpoints;

            public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
                TestEventOne message,
                MessageHeaderCollection headers) =>
                message.Content?.Split(',')
                    .Select(int.Parse)
                    .Select(index => _endpoints[index - 1]) ??
                Enumerable.Empty<IProducerEndpoint>();
        }
    }
}
