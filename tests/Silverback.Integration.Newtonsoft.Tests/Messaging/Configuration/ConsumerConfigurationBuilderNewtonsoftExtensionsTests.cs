// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class ConsumerConfigurationBuilderNewtonsoftExtensionsTests
{
    [Fact]
    public void DeserializeJsonUsingNewtonsoft_Default_DeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft().Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<object>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_WithSetMessageType_TypedDeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft().Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseModelWithGenericArgument_DeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration configuration = builder
            .DeserializeJsonUsingNewtonsoft(deserializer => deserializer.UseModel<object>())
            .Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<object>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseModel_DeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration configuration = builder
            .DeserializeJsonUsingNewtonsoft(deserializer => deserializer.UseModel(typeof(TestEventOne)))
            .Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_Configure_DeserializerAndOptionsSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            deserializer => deserializer.Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })).Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        configuration.Deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().Settings.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_WithEncoding_DeserializerAndEncodingSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
                deserializer => deserializer
                    .WithEncoding(MessageEncoding.Unicode))
            .Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        configuration.Deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().Encoding.Should()
            .Be(MessageEncoding.Unicode);
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseModelAndConfigure_DeserializerAndOptionsSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new();

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            deserializer => deserializer
                .UseModel<object>()
                .Configure(
                    settings =>
                    {
                        settings.MaxDepth = 42;
                    })).Build();

        configuration.Deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        configuration.Deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().Settings.MaxDepth.Should()
            .Be(42);
    }
}
