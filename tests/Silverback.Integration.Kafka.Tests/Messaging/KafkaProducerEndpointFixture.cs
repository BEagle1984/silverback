// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaProducerEndpointFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        KafkaProducerEndpoint endpoint = new("topic", 42, new KafkaProducerEndpointConfiguration());

        endpoint.RawName.Should().Be("topic");
    }
}
