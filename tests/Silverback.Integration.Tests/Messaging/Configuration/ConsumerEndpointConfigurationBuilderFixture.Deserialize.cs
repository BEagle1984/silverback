// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using FluentAssertions;
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
    public void Build_ShouldSetJsonMessageDeserializerByDefault()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
        endpoint.Deserializer.Should().NotBeSameAs(DefaultDeserializers.Json);
    }

    [Fact]
    public void Build_ShouldSetTypedJsonMessageDeserializerByDefault_WhenMessageTypeIsNotObject()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageDeserializerByDefault_WhenMessageTypeIsBinaryMessage()
    {
        TestConsumerEndpointConfigurationBuilder<BinaryMessage> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.Should().BeOfType<BinaryMessageDeserializer<BinaryMessage>>();
        endpoint.Deserializer.Should().NotBeSameAs(DefaultDeserializers.Json);
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageDeserializerByDefault_WhenMessageTypeImplementsIBinaryMessage()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.Should().BeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
        endpoint.Deserializer.Should().NotBeSameAs(DefaultDeserializers.Json);
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedDeserializer_WhenMessageTypeIsNotObject()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedDeserializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(deserializer => deserializer.UseModel<TestEventOne>())
            .Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(deserializer => deserializer.UseModel(typeof(TestEventOne)))
            .Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetDeserializerAndOptions_WhenOptionsAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson(
                deserializer => deserializer.Configure(
                    options =>
                    {
                        options.MaxDepth = 42;
                    }))
            .Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
        endpoint.Deserializer.As<JsonMessageDeserializer<object>>().Options!.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void DeserializeJson_ShouldSetDeserializerAndOptions_WhenFixedTypeAndOptionsAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(
                deserializer => deserializer
                    .UseModel<TestEventOne>()
                    .Configure(
                        options =>
                        {
                            options.MaxDepth = 42;
                        }))
            .Build();

        endpoint.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
        endpoint.Deserializer.As<JsonMessageDeserializer<TestEventOne>>().Options!.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Deserializer.Should().BeOfType<BinaryMessageDeserializer<BinaryMessage>>();
        endpoint.Deserializer.Should().NotBeSameAs(DefaultDeserializers.Binary);
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetCustomTypeDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Deserializer.Should().BeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
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
    public void ConsumeBinaryMessages_ShouldSetCustomTypeDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration endpoint = builder
            .ConsumeBinaryMessages(deserializer => deserializer.UseModel<CustomBinaryMessage>())
            .Build();

        endpoint.Deserializer.Should().BeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
    }

    private sealed class CustomBinaryMessage : IBinaryMessage
    {
        public Stream? Content { get; set; }
    }

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
