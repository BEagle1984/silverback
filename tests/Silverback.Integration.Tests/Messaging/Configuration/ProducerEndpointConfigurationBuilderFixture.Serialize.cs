// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public partial class ProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldSetJsonMessageSerializerByDefault()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        configuration.Serializer.ShouldNotBeSameAs(DefaultSerializers.Json);
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageSerializerByDefault_WhenMessageTypeIsBinaryMessage()
    {
        TestProducerEndpointConfigurationBuilder<BinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.Serializer.ShouldBeOfType<BinaryMessageSerializer>();
        configuration.Serializer.ShouldNotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void Build_ShouldSetBinaryMessageSerializerByDefault_WhenMessageTypeImplementsIBinaryMessage()
    {
        TestProducerEndpointConfigurationBuilder<CustomBinaryMessage> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.Serializer.ShouldBeOfType<BinaryMessageSerializer>();
        configuration.Serializer.ShouldNotBeSameAs(DefaultSerializers.Binary);
    }

    [Fact]
    public void Build_ShouldSetStringMessageSerializerByDefault_WhenMessageTypeIsStringMessage()
    {
        TestProducerEndpointConfigurationBuilder<StringMessage> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.Build();

        configuration.Serializer.ShouldBeOfType<StringMessageSerializer>();
    }

    [Fact]
    public void SerializeAsJson_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJson().Build();

        configuration.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void SerializeAsJson_ShouldSetSerializerAndOptions()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJson(
            serializer => serializer.Configure(
                options =>
                {
                    options.MaxDepth = 42;
                })).Build();

        JsonMessageSerializer jsonSerializer = configuration.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        jsonSerializer.Options!.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void ProduceBinaryMessages_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ProduceBinaryMessages().Build();

        configuration.Serializer.ShouldBeOfType<BinaryMessageSerializer>();
    }

    [Fact]
    public void ProduceStrings_ShouldSetSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ProduceStrings().Build();

        configuration.Serializer.ShouldBeOfType<StringMessageSerializer>();
    }

    [Fact]
    public void ProduceStrings_ShouldSetConfiguredSerializer()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.ProduceStrings(
            serializer => serializer
                .WithEncoding(MessageEncoding.ASCII)).Build();

        StringMessageSerializer stringSerializer = configuration.Serializer.ShouldBeOfType<StringMessageSerializer>();
        stringSerializer.Encoding.ShouldBe(MessageEncoding.ASCII);
    }

    private sealed class CustomBinaryMessage : IBinaryMessage
    {
        public Stream? Content { get; set; }
    }
}
