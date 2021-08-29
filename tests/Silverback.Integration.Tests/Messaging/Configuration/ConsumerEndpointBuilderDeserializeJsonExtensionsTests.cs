// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ConsumerEndpointBuilderDeserializeJsonExtensionsTests
    {
        [Fact]
        public void DeserializeJson_Default_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeJson().Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
            endpoint.Serializer.Should().NotBeSameAs(JsonMessageSerializer.Default);
        }

        [Fact]
        public void DeserializeJson_WithSetMessageType_TypedSerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder(typeof(TestEventOne));

            var endpoint = builder.DeserializeJson().Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        }

        [Fact]
        public void DeserializeJson_UseFixedTypeWithGenericArgument_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeJson(serializer => serializer.UseFixedType<TestEventOne>())
                .Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        }

        [Fact]
        public void DeserializeJson_UseFixedType_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder
                .DeserializeJson(serializer => serializer.UseFixedType(typeof(TestEventOne))).Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        }

        [Fact]
        public void DeserializeJson_WithOptions_SerializerAndOptionsSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeJson(
                serializer => serializer.WithOptions(
                    new JsonSerializerOptions
                    {
                        MaxDepth = 42
                    })).Build();

            endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
            endpoint.Serializer.As<JsonMessageSerializer>().Options.MaxDepth.Should().Be(42);
        }

        [Fact]
        public void DeserializeJson_UseFixedTypeWithOptions_SerializerAndOptionsSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.DeserializeJson(
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
