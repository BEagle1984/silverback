// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class AvroMessageDeserializerBuilderFixture
{
    [Fact]
    public void Configure_ShouldSetSchemaRegistryAndSerializerConfig_WhenConfigureIsCalled()
    {
        AvroMessageDeserializerBuilder builder = new();

        IMessageSerializer serializer = builder
            .UseType<TestEventOne>()
            .Configure(
                schemaRegistryConfig =>
                {
                    schemaRegistryConfig.Url = "some-url";
                },
                serializerConfig =>
                {
                    serializerConfig.CancellationDelayMaxMs = 42;
                })
            .Build();

        serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
        serializer.As<AvroMessageDeserializer<TestEventOne>>().SchemaRegistryConfig.Url.Should().Be("some-url");
    }
}
