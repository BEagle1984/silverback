// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Sequences.Chunking;

public class KafkaChunkEnricherTests
{
    private readonly KafkaProducerEndpointConfiguration _endpointConfiguration =
        new KafkaProducerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://tests")
            .Produce<object>(endpoint => endpoint.ProduceTo("topic1"))
            .Build()
            .Endpoints.Single();

    [Fact]
    public void GetFirstChunkMessageHeader_ShouldReturnFirstChunkOffsetHeader_WhenIdentifierIsKafkaOffset()
    {
        OutboundEnvelope envelope = new(null, null, _endpointConfiguration, Substitute.For<IProducer>(), null, new KafkaOffset("topic1", 2, 42));

        MessageHeader? header = new KafkaChunkEnricher().GetFirstChunkMessageHeader(envelope);

        header.ShouldNotBeNull();
        header.Name.ShouldBe(KafkaMessageHeaders.FirstChunkOffset);
        header.Value.ShouldBe("42");
    }

    [Fact]
    public void GetFirstChunkMessageHeader_ShouldReturnNull_WhenIdentifierIsNull()
    {
        OutboundEnvelope envelope = new(null, null, _endpointConfiguration, Substitute.For<IProducer>());

        MessageHeader? header = new KafkaChunkEnricher().GetFirstChunkMessageHeader(envelope);

        header.ShouldBeNull();
    }

    [Fact]
    public void GetFirstChunkMessageHeader_ShouldReturnNull_WhenIdentifierIsNotKafkaOffset()
    {
        OutboundEnvelope envelope = new(null, null, _endpointConfiguration, Substitute.For<IProducer>(), null, new TestOffset());

        MessageHeader? header = new KafkaChunkEnricher().GetFirstChunkMessageHeader(envelope);

        header.ShouldBeNull();
    }
}
