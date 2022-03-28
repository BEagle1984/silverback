// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ConsumerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldSetJsonMessageSerializerByDefault()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void Build_ShouldSetTypedJsonMessageSerializerByDefault_WhenMessageTypeIsNotObject()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageSerializerByDefault_WhenMessageTypeIsBinaryMessage()
    {
        TestConsumerEndpointConfigurationBuilder<BinaryMessage> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<BinaryMessage>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageSerializerByDefault_WhenMessageTypeImplementsIBinaryMessage()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetSerializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedSerializer_WhenMessageTypeIsNotObject()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedSerializer_WhenUseFixedTypeWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(serializer => serializer.UseFixedType<TestEventOne>())
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedSerializer_WhenUseFixedTypeIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(serializer => serializer.UseFixedType(typeof(TestEventOne)))
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetSerializerAndOptions_WhenOptionsAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson(serializer => serializer.WithOptions(new JsonSerializerOptions { MaxDepth = 42 }))
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
        endpoint.Serializer.As<JsonMessageSerializer<object>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void DeserializeJson_ShouldSetSerializerAndOptions_WhenFixedTypeAndOptionsAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(
                serializer => serializer
                    .UseFixedType<TestEventOne>()
                    .WithOptions(new JsonSerializerOptions { MaxDepth = 42 }))
            .Build();

        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        endpoint.Serializer.As<JsonMessageSerializer<TestEventOne>>().Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetSerializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<BinaryMessage>>();
        endpoint.Serializer.Should().NotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetCustomTypeSerializer()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldThrow_WhenTheMessageTypeIsNotValid()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        Action act = () => builder.ConsumeBinaryMessages();

        act.Should().ThrowExactly<ArgumentException>().WithMessage("The type *.TestEventOne does not implement IBinaryMessage. *");
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldThrow_WhenMessageDoesNotHaveDefaultConstructor()
    {
        Action act = () =>
        {
            TestConsumerEndpointConfigurationBuilder<BinaryMessageWithoutDefaultConstructor> builder = new();
            builder.ConsumeBinaryMessages();
        };

        act.Should().ThrowExactly<ArgumentException>()
            .WithMessage("The type *+BinaryMessageWithoutDefaultConstructor does not have a default constructor. *");
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetCustomTypeSerializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .ConsumeBinaryMessages(serializer => serializer.UseModel<CustomBinaryMessage>())
            .Build();

        endpoint.Serializer.Should().BeOfType<BinaryMessageSerializer<CustomBinaryMessage>>();
    }

    private sealed class CustomBinaryMessage : IBinaryMessage
    {
        public Stream? Content { get; set; }
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class BinaryMessageWithoutDefaultConstructor : IBinaryMessage
    {
        public BinaryMessageWithoutDefaultConstructor(string content)
        {
            Content = new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public Stream? Content { get; set; }
    }
}
