// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ProducerEndpointBuilderSerializeAsJsonExtensionsTests
    {
        [Fact]
        public void SerializeAsJson_Default_SerializerSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.SerializeAsJson().Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
            endpoint.Serializer.Should().NotBeSameAs(JsonMessageSerializer.Default);
        }

        [Fact]
        public void SerializeAsJson_UseFixedType_SerializerSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.SerializeAsJson(serializer => serializer.UseFixedType<TestEventOne>()).Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        }

        [Fact]
        public void SerializeAsJson_WithOptions_SerializerAndOptionsSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.SerializeAsJson(
                serializer => serializer.WithOptions(
                    new JsonSerializerOptions
                    {
                        MaxDepth = 42
                    })).Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
            endpoint.Serializer.As<JsonMessageSerializer>().Options.MaxDepth.Should().Be(42);
        }

        [Fact]
        public void SerializeAsJson_UseFixedTypeWithOptions_SerializerAndOptionsSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.SerializeAsJson(
                serializer => serializer
                    .UseFixedType<TestEventOne>()
                    .WithOptions(
                        new JsonSerializerOptions
                        {
                            MaxDepth = 42
                        })).Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
            endpoint.Serializer.As<JsonMessageSerializer<TestEventOne>>().Options.MaxDepth.Should().Be(42);
        }
    }
}
