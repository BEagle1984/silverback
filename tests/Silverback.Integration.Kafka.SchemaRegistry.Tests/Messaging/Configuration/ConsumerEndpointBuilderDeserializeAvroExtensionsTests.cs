// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration
{
    public class ConsumerEndpointBuilderDeserializeAvroExtensionsTests
    {
        [Fact]
        public void DeserializeAvro_WithoutType_ExceptionThrown()
        {
            var builder = new TestConsumerEndpointBuilder();

            Action act = () => builder.DeserializeAvro();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void DeserializeAvro_Default_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeAvro(serializer => serializer.UseType<TestEventOne>())
                .Build();

            endpoint.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
        }

        [Fact]
        public void DeserializeAvro_Configure_SchemaRegistryAndSerializerConfigSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeAvro(
                serializer => serializer
                    .UseType<TestEventOne>()
                    .Configure(
                        schemaRegistryConfig => { schemaRegistryConfig.Url = "some-url"; },
                        serializerConfig => { serializerConfig.BufferBytes = 42; })).Build();

            endpoint.Serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
            endpoint.Serializer.As<AvroMessageSerializer<TestEventOne>>().SchemaRegistryConfig.Url.Should()
                .Be("some-url");
            endpoint.Serializer.As<AvroMessageSerializer<TestEventOne>>().AvroSerializerConfig.BufferBytes.Should()
                .Be(42);
        }
    }
}
