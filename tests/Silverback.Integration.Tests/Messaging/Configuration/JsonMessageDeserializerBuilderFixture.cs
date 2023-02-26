// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class JsonMessageDeserializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultDeserializer()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.Build();

        deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
        deserializer.As<JsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel(typeof(TestEventOne)).Build();

        deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void Configure_ShouldSetOptions()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .Configure(
                options =>
                {
                    options.MaxDepth = 42;
                })
            .Build();

        deserializer.As<JsonMessageDeserializer<object>>().Options!.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void WithOptionalMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithOptionalMessageTypeHeader().Build();

        deserializer.As<JsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void WithMandatoryMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithMandatoryMessageTypeHeader().Build();

        deserializer.As<JsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Mandatory);
    }

    [Fact]
    public void IgnoreMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.IgnoreMessageTypeHeader().Build();

        deserializer.As<JsonMessageDeserializer<object>>().TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Ignore);
    }
}
