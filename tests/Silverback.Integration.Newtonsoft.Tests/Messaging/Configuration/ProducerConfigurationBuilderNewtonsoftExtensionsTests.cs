// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class ProducerConfigurationBuilderNewtonsoftExtensionsTests
{
    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_Default_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft().Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_WithSetMessageType_SerializerSet()
    {
        TestProducerConfigurationBuilder<TestEventOne> builder = new();

        TestProducerConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft().Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_UseFixedTypeWithGenericArgument_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration configuration = builder
            .SerializeAsJsonUsingNewtonsoft(serializer => serializer.UseFixedType<TestEventOne>())
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_UseFixedType_SerializerSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration configuration = builder
            .SerializeAsJsonUsingNewtonsoft(serializer => serializer.UseFixedType(typeof(TestEventOne)))
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_Configure_SerializerAndOptionsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
            serializer => serializer.Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })).Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer<object>>().Settings.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_WithEncoding_SerializerAndOptionsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
                serializer => serializer
                    .WithEncoding(MessageEncoding.Unicode))
            .Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<object>>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer<object>>().Encoding.Should().Be(MessageEncoding.Unicode);
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_UseFixedTypeAndConfigure_SerializerAndOptionsSet()
    {
        TestProducerConfigurationBuilder<object> builder = new();

        TestProducerConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
            serializer => serializer
                .UseFixedType<TestEventOne>()
                .Configure(
                    settings =>
                    {
                        settings.MaxDepth = 42;
                    })).Build();

        configuration.Serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer<TestEventOne>>();
        configuration.Serializer.As<NewtonsoftJsonMessageSerializer<TestEventOne>>().Settings.MaxDepth.Should()
            .Be(42);
    }
}
