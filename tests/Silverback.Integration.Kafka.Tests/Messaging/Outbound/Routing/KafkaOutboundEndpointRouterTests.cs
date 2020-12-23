// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound.Routing
{
    public class KafkaOutboundEndpointRouterTests
    {
        [Fact]
        public void GetDestinationEndpoints_SingleEndpointRouter_CorrectEndpointReturned()
        {
            var router = new KafkaOutboundEndpointRouter<TestMessage>(
                (message, _, endpoints) => message.Content switch
                {
                    "first" => endpoints["one"],
                    "second" => endpoints["two"],
                    "third" => endpoints["three"],
                    _ => throw new ArgumentOutOfRangeException()
                },
                new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                {
                    { "one", builder => builder.ProduceTo("topic1") },
                    { "two", builder => builder.ProduceTo("topic2") },
                    { "three", builder => builder.ProduceTo("topic3") }
                },
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever"
                });

            var topics1 = router.GetDestinationEndpoints(
                new TestMessage { Content = "first" },
                new MessageHeaderCollection());
            var topics2 = router.GetDestinationEndpoints(
                new TestMessage { Content = "second" },
                new MessageHeaderCollection());
            var topics3 = router.GetDestinationEndpoints(
                new TestMessage { Content = "third" },
                new MessageHeaderCollection());

            topics1.First().Name.Should().Be("topic1");
            topics2.First().Name.Should().Be("topic2");
            topics3.First().Name.Should().Be("topic3");
        }

        [Fact]
        public void GetDestinationEndpoints_MultipleEndpointRouter_CorrectEndpointReturned()
        {
            var router = new KafkaOutboundEndpointRouter<TestMessage>(
                (message, _, endpoints) => message.Content switch
                {
                    "first" => endpoints.Values.Take(1),
                    "second" => endpoints.Values.Take(2),
                    "third" => endpoints.Values.Take(3),
                    _ => throw new ArgumentOutOfRangeException()
                },
                new Dictionary<string, Action<IKafkaProducerEndpointBuilder>>
                {
                    { "one", builder => builder.ProduceTo("topic1") },
                    { "two", builder => builder.ProduceTo("topic2") },
                    { "three", builder => builder.ProduceTo("topic3") }
                },
                new KafkaClientConfig
                {
                    BootstrapServers = "PLAINTEXT://whatever"
                });

            var topics1 = router.GetDestinationEndpoints(
                new TestMessage { Content = "first" },
                new MessageHeaderCollection()).ToList();
            var topics2 = router.GetDestinationEndpoints(
                new TestMessage { Content = "second" },
                new MessageHeaderCollection()).ToList();
            var topics3 = router.GetDestinationEndpoints(
                new TestMessage { Content = "third" },
                new MessageHeaderCollection()).ToList();

            topics1.Should().HaveCount(1);
            topics1.Select(endpoint => endpoint.Name).Should().BeEquivalentTo("topic1");
            topics2.Should().HaveCount(2);
            topics2.Select(endpoint => endpoint.Name).Should().BeEquivalentTo("topic1", "topic2");
            topics3.Should().HaveCount(3);
            topics3.Select(endpoint => endpoint.Name).Should().BeEquivalentTo("topic1", "topic2", "topic3");
        }
    }
}
