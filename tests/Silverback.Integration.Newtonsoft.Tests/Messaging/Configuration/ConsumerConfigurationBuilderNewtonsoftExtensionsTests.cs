﻿// Copyright (c) 2020 Sergio Aquilini
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
    public void DeserializeJsonUsingNewtonsoft_Default_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft().Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_WithSetMessageType_TypedSerializerSet()
    {
        TestConsumerConfigurationBuilder<TestEventOne> builder = new();

        TestConsumerConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft().Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseFixedTypeWithGenericArgument_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration configuration = builder
            .DeserializeJsonUsingNewtonsoft(serializer => serializer.UseFixedType<object>())
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseFixedType_SerializerSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration configuration = builder
            .DeserializeJsonUsingNewtonsoft(serializer => serializer.UseFixedType(typeof(TestEventOne)))
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_Configure_SerializerAndOptionsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            serializer => serializer.Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })).Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer<object>>().Settings.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_WithEncoding_SerializerAndEncodingSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            serializer => serializer
                .WithEncoding(MessageEncoding.Unicode))
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer<object>>().Encoding.Should()
            .Be(MessageEncoding.Unicode);
    }

    [Fact]
    public void DeserializeJsonUsingNewtonsoft_UseFixedTypeAndConfigure_SerializerAndOptionsSet()
    {
        TestConsumerConfigurationBuilder<object> builder = new();

        TestConsumerConfiguration configuration = builder.DeserializeJsonUsingNewtonsoft(
            serializer => serializer
                .UseFixedType<object>()
                .Configure(
                    settings =>
                    {
                        settings.MaxDepth = 42;
                    })).Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer<object>>().Settings.MaxDepth.Should()
            .Be(42);
    }
}
