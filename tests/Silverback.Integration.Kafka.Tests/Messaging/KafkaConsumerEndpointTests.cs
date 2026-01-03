// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging;

public class KafkaConsumerEndpointTests
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        KafkaConsumerEndpoint endpoint = new("topic", 42, new KafkaConsumerEndpointConfiguration());

        endpoint.RawName.ShouldBe("topic");
    }
}
