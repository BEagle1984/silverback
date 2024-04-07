// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class AvroMessageSerializerBuilderFixture
{
    [Fact]
    public void Configure_ShouldSetSchemaRegistryAndSerializerConfig_WhenConfigureIsCalled()
    {
        AvroMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder
            .UseModel<TestEventOne>()
            .Configure(
                schemaRegistryConfig =>
                {
                    schemaRegistryConfig.Url = "some-url";
                },
                serializerConfig =>
                {
                    serializerConfig.BufferBytes = 42;
                }).Build();

        serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
        serializer.As<AvroMessageSerializer<TestEventOne>>().SchemaRegistryConfig.Url.Should().Be("some-url");
        serializer.As<AvroMessageSerializer<TestEventOne>>().AvroSerializerConfig.BufferBytes.Should().Be(42);
    }
}
