// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class ProducerConfigurationBuilderNewtonsoftExtensionsTests
{
    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_Default_SerializerSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft().Build();

        configuration.Serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_Configure_SerializerAndOptionsSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
            serializer => serializer.Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })).Build();

        configuration.Serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
        NewtonsoftJsonMessageSerializer newtonsoftJsonMessageSerializer = configuration.Serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
        newtonsoftJsonMessageSerializer.Settings.ShouldNotBeNull();
        newtonsoftJsonMessageSerializer.Settings.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void SerializeAsJsonUsingNewtonsoft_WithEncoding_SerializerAndOptionsSet()
    {
        TestProducerEndpointConfigurationBuilder<object> builder = new(Substitute.For<IServiceProvider>());

        TestProducerEndpointConfiguration configuration = builder.SerializeAsJsonUsingNewtonsoft(
                serializer => serializer
                    .WithEncoding(MessageEncoding.Unicode))
            .Build();

        configuration.Serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
        configuration.Serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>().Encoding.ShouldBe(MessageEncoding.Unicode);
    }
}
