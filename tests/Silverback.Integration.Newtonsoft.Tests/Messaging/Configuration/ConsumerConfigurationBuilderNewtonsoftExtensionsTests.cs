// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
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
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft().Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_WithSetMessageType_TypedDeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft().Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseModelWithGenericArgument_DeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder
            .DeserializeJsonUsingNewtonsoft(deserializer => deserializer.UseModel<object>())
            .Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseModel_DeserializerSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder
            .DeserializeJsonUsingNewtonsoft(deserializer => deserializer.UseModel(typeof(TestEventOne)))
            .Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_Configure_DeserializerAndOptionsSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            deserializer => deserializer.Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })).Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        NewtonsoftJsonMessageDeserializer<object> newtonsoftJsonMessageDeserializer = configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        newtonsoftJsonMessageDeserializer.Settings.ShouldNotBeNull();
        newtonsoftJsonMessageDeserializer.Settings.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_WithEncoding_DeserializerAndEncodingSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
                deserializer => deserializer
                    .WithEncoding(MessageEncoding.Unicode))
            .Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>().Encoding.ShouldBe(MessageEncoding.Unicode);
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseModelAndConfigure_DeserializerAndOptionsSet()
    {
        TestConsumerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestConsumerEndpointConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            deserializer => deserializer
                .UseModel<object>()
                .Configure(
                    settings =>
                    {
                        settings.MaxDepth = 42;
                    })).Build();

        configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        NewtonsoftJsonMessageDeserializer<object> newtonsoftJsonMessageDeserializer = configuration.Deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        newtonsoftJsonMessageDeserializer.Settings.ShouldNotBeNull();
        newtonsoftJsonMessageDeserializer.Settings.MaxDepth.ShouldBe(42);
    }
}
