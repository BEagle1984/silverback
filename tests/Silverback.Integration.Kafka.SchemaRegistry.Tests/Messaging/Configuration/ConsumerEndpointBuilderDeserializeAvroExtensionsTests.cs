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
        public void DeserializeAvro_WithSetMessageType_TypedSerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder(typeof(TestEventOne));

            var endpoint = builder.DeserializeAvro().Build();

            endpoint.Serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
        }

        [Fact]
        public void DeserializeAvro_UseTypeWithGenericArgument_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeAvro(serializer => serializer.UseType<TestEventOne>())
                .Build();

            endpoint.Serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
        }

        [Fact]
        public void DeserializeAvro_UseType_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeAvro(serializer => serializer.UseType(typeof(TestEventOne)))
                .Build();

            endpoint.Serializer.Should().BeOfType<AvroMessageDeserializer<TestEventOne>>();
        }
    }
}
