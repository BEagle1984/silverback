// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerConfigurationBuilderTests
{
    [Fact]
    public void ImplicitSerializeAsJson_Default_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(EndpointConfiguration.DefaultSerializer);
    }

    [Fact]
    public void ImplicitSerializeAsJson_WithSetMessageType_TypedSerializerSet()
    {
        TestProducerConfigurationBuilder<TestEventOne> builder = new();

        TestProducerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_Default_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.SerializeAsJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(EndpointConfiguration.DefaultSerializer);
    }

    [Fact]
    public void SerializeAsJson_WithSetMessageType_TypedSerializerSet()
    {
        TestProducerConfigurationBuilder<TestEventOne> builder = new();

        TestProducerConfiguration endpoint = builder.SerializeAsJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_UseFixedTypeWithGenericArgument_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.SerializeAsJson(serializer => serializer.UseFixedType<TestEventOne>())
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_UseFixedType_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder
            .SerializeAsJson(serializer => serializer.UseFixedType(typeof(TestEventOne)))
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJson_WithOptions_SerializerAndOptionsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.SerializeAsJson(
            serializer => serializer.WithOptions(
                new JsonSerializerOptions
                {
                    MaxDepth = 42
                })).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.As<JsonMessageSerializer<object>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void SerializeAsJson_UseFixedTypeWithOptions_SerializerAndOptionsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.SerializeAsJson(
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

    [Fact]
    public void ProduceBinaryFiles_Default_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.ProduceBinaryFiles().Build();

        endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer>();
    }

    [Fact]
    public void ProduceBinaryFiles_UseFixedType_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration endpoint = builder.ProduceBinaryFiles(serializer => serializer.UseModel<CustomBinaryFileMessage>())
            .Build();

        endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
    }

    private sealed class CustomBinaryFileMessage : BinaryFileMessage
    {
    }
}
