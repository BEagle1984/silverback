// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaConsumerEndpointFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        KafkaConsumerEndpoint endpoint = new("topic", 42, new KafkaConsumerEndpointConfiguration());

        endpoint.RawName.Should().Be("topic");
    }
}
