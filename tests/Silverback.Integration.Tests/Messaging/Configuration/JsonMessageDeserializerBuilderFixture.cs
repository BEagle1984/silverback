// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
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

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel(typeof(TestEventOne)).Build();

        deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
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

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options!.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void WithOptionalMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithOptionalMessageTypeHeader().Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void WithMandatoryMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithMandatoryMessageTypeHeader().Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Mandatory);
    }

    [Fact]
    public void IgnoreMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.IgnoreMessageTypeHeader().Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Ignore);
    }
}
