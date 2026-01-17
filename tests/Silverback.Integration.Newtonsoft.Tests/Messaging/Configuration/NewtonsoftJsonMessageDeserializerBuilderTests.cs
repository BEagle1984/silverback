// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class NewtonsoftJsonMessageDeserializerBuilderTests
{
    [Fact]
    public void Build_ShouldReturnDefaultDeserializer_WhenTypeNotSpecified()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.Build();

        NewtonsoftJsonMessageDeserializer<object> newtonsoftDeserializer = deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        newtonsoftDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Mandatory);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        NewtonsoftJsonMessageDeserializer<TestEventOne> newtonsoftDeserializer = deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
        newtonsoftDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Ignore);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<TestEventOne>>();
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

        NewtonsoftJsonMessageDeserializer<object> newtonsoftJsonMessageDeserializer = deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>();
        newtonsoftJsonMessageDeserializer.Settings.ShouldNotBeNull();
        newtonsoftJsonMessageDeserializer.Settings.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void WithOptionalMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithOptionalMessageTypeHeader().Build();

        deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public void WithMandatoryMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithMandatoryMessageTypeHeader().Build();

        deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Mandatory);
    }

    [Fact]
    public void IgnoreMessageTypeHeader_ShouldSetTypeHeaderBehavior()
    {
        NewtonsoftJsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.IgnoreMessageTypeHeader().Build();

        deserializer.ShouldBeOfType<NewtonsoftJsonMessageDeserializer<object>>().TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Ignore);
    }
}
