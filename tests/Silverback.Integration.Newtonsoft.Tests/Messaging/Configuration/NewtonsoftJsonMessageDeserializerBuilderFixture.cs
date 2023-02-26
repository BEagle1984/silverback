// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class NewtonsoftJsonMessageDeserializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultDeserializer()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.Build();

        deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel(typeof(TestEventOne)).Build();

        deserializer.Should().BeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void Configure_ShouldSetSettings()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })
            .Build();

        deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().Settings.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void WithOptionalMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithOptionalMessageTypeHeader().Build();

        deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void WithMandatoryMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithMandatoryMessageTypeHeader().Build();

        deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Mandatory);
    }

    [Fact]
    public void IgnoreMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.IgnoreMessageTypeHeader().Build();

        deserializer.As<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Ignore);
    }
}
