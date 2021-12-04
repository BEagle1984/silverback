// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
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

public partial class ConsumerConfigurationBuilderTests
{
    [Fact]
    public void ImplicitDeserializeJson_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(EndpointConfiguration.DefaultSerializer);
    }

    [Fact]
    public void ImplicitDeserializeJson_WithSetMessageType_TypedSerializerSet()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(EndpointConfiguration.DefaultSerializer);
    }

    [Fact]
    public void DeserializeJson_WithSetMessageType_TypedSerializerSet()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_UseFixedTypeWithGenericArgument_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson(serializer => serializer.UseFixedType<TestEventOne>())
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_UseFixedType_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder
            .DeserializeJson(serializer => serializer.UseFixedType(typeof(TestEventOne))).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_WithOptions_SerializerAndOptionsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson(
            serializer => serializer.WithOptions(
                new JsonSerializerOptions
                {
                    MaxDepth = 42
                })).Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.As<JsonMessageSerializer<object>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void DeserializeJson_UseFixedTypeWithOptions_SerializerAndOptionsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.DeserializeJson(
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
    public void ConsumeBinaryFiles_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder.ConsumeBinaryFiles().Build();

        endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer>();
        endpoint.Serializer.Should().NotBeSameAs(BinaryFileMessageSerializer.Default);
    }

    [Fact]
    public void ConsumeBinaryFiles_WithSetMessageType_SerializerSet()
    {
        TestConsumerConfigurationBuilder<CustomBinaryFileMessage> builder = new();

        TestConsumerConfiguration endpoint = builder.ConsumeBinaryFiles().Build();

        endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
    }

    [Fact]
    public void ConsumeBinaryFiles_WithInvalidMessageType_ExceptionThrown()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        Action act = () => builder.ConsumeBinaryFiles();

        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("The type *.TestEventOne does not implement IBinaryFileMessage. *");
    }

    [Fact]
    public void ConsumeBinaryFiles_MessageTypeWithoutEmptyConstructor_ExceptionThrown()
    {
        TestConsumerConfigurationBuilder<BinaryFileMessageWithoutDefaultConstructor> builder = new();

        Action act = () => builder.ConsumeBinaryFiles();

        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("The type *+BinaryFileMessageWithoutDefaultConstructor does not have a default constructor. *");
    }

    [Fact]
    public void ConsumeBinaryFiles_UseModel_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration endpoint = builder
            .ConsumeBinaryFiles(serializer => serializer.UseModel<CustomBinaryFileMessage>())
            .Build();

        endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
    }

    private sealed class CustomBinaryFileMessage : BinaryFileMessage
    {
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class BinaryFileMessageWithoutDefaultConstructor : BinaryFileMessage
    {
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required for testing.")]
        public BinaryFileMessageWithoutDefaultConstructor(string value)
        {
        }
    }
}
