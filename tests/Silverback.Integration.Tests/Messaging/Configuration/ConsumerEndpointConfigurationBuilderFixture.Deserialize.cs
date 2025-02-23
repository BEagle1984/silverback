// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using NSubstitute;
using Shouldly;
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
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        endpoint.Deserializer.ShouldNotBeSameAs(DefaultDeserializers.Json);
    }

    [Fact]
    public void Build_ShouldSetTypedJsonMessageDeserializerByDefault_WhenMessageTypeIsNotObject()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageDeserializerByDefault_WhenMessageTypeIsBinaryMessage()
    {
        TestConsumerEndpointConfigurationBuilder<BinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.ShouldBeOfType<BinaryMessageDeserializer<BinaryMessage>>();
        endpoint.Deserializer.ShouldNotBeSameAs(DefaultDeserializers.Binary);
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageDeserializerByDefault_WhenMessageTypeImplementsIBinaryMessage()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.ShouldBeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
        endpoint.Deserializer.ShouldNotBeSameAs(DefaultDeserializers.Binary);
    }

    [Fact]
    public void Build_ShouldSetStringMessageDeserializerByDefault_WhenMessageTypeIsStringMessage()
    {
        TestConsumerEndpointConfigurationBuilder<StringMessage> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage>>();
    }

    [Fact]
    public void Build_ShouldSetStringMessageDeserializerByDefault_WhenMessageTypeIsTypedStringMessage()
    {
        TestConsumerEndpointConfigurationBuilder<StringMessage<TestEventOne>> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.Build();

        endpoint.Deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage<TestEventOne>>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        endpoint.Deserializer.ShouldNotBeSameAs(DefaultDeserializers.Json);
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedDeserializer_WhenMessageTypeIsNotObject()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson().Build();

        endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedDeserializer_WhenUseModelWithGenericArgumentIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(deserializer => deserializer.UseModel<TestEventOne>())
            .Build();

        endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetTypedDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder
            .DeserializeJson(deserializer => deserializer.UseModel(typeof(TestEventOne)))
            .Build();

        endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJson_ShouldSetDeserializerAndOptions_WhenOptionsAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.DeserializeJson(
                deserializer => deserializer.Configure(
                    options =>
                    {
                        options.MaxDepth = 42;
                    }))
            .Build();

        JsonMessageDeserializer<object> jsonDeserializer = endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options!.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void DeserializeJson_ShouldSetDeserializerAndOptions_WhenFixedTypeAndOptionsAreSpecified()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

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

        JsonMessageDeserializer<TestEventOne> jsonDeserializer = endpoint.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        jsonDeserializer.Options!.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Deserializer.ShouldBeOfType<BinaryMessageDeserializer<BinaryMessage>>();
        endpoint.Deserializer.ShouldNotBeSameAs(DefaultDeserializers.Binary);
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetCustomTypeDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Deserializer.ShouldBeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldThrow_WhenTheMessageTypeIsNotValid()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.ConsumeBinaryMessages();

        Exception exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldMatch(@"The type .*\.TestEventOne does not implement IBinaryMessage\. .*");
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldThrow_WhenMessageDoesNotHaveDefaultConstructor()
    {
        Action act = () =>
        {
            TestConsumerEndpointConfigurationBuilder<BinaryMessageWithoutDefaultConstructor> builder = new(Substitute.For<IServiceProvider>());
            builder.ConsumeBinaryMessages();
        };

        Exception exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldMatch(@"The type .*\+BinaryMessageWithoutDefaultConstructor does not have a default constructor\. .*");
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetCustomTypeDeserializer_WhenUseModelIsCalled()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder
            .ConsumeBinaryMessages(deserializer => deserializer.UseModel<CustomBinaryMessage>())
            .Build();

        endpoint.Deserializer.ShouldBeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void ConsumeBinaryMessages_ShouldSetTypeDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeBinaryMessages().Build();

        endpoint.Deserializer.ShouldBeOfType<BinaryMessageDeserializer<CustomBinaryMessage>>();
    }

    [Fact]
    public void ConsumeStrings_ShouldSetDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeStrings().Build();

        endpoint.Deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage>>();
    }

    [Fact]
    public void ConsumeStrings_ShouldSetTypeDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeStrings().Build();

        endpoint.Deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage<TestEventOne>>>();
    }

    [Fact]
    public void ConsumeStrings_ShouldSetConfiguredDeserializer()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration endpoint = builder.ConsumeStrings(
            serializer => serializer
                .UseDiscriminator<TestEventTwo>()
                .WithEncoding(MessageEncoding.ASCII)).Build();

        StringMessageDeserializer<StringMessage<TestEventTwo>> stringDeserializer =
            endpoint.Deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage<TestEventTwo>>>();
        stringDeserializer.Encoding.ShouldBe(MessageEncoding.ASCII);
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
