// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class JsonMessageDeserializerBuilderTests
{
    [Fact]
    public void Build_ShouldReturnDefaultDeserializer()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Mandatory);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        JsonMessageDeserializer<TestEventOne> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        jsonDeserializer.TypeHeaderBehavior.ShouldBe(JsonMessageDeserializerTypeHeaderBehavior.Ignore);
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseModel<TestEventOne>().Build();

        deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void Configure_ShouldSetOptions()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .Configure(options =>
            {
                options.MaxDepth = 42;
            })
            .Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options!.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void Configure_ShouldApplyToPreviousOptions_WhenCalledMultipleTimes()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .Configure(options => options.MaxDepth = 42)
            .Configure(options => options.AllowTrailingCommas = true)
            .Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options.ShouldNotBeNull();
        jsonDeserializer.Options.MaxDepth.ShouldBe(42);
        jsonDeserializer.Options.AllowTrailingCommas.ShouldBeTrue();
    }

    [Fact]
    public void Configure_ShouldNotAlterPreviousOptions()
    {
        JsonSerializerOptions? firstOptions = null;
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .Configure(
                options =>
                {
                    options.MaxDepth = 42;
                    firstOptions = options;
                })
            .Configure(options => options.MaxDepth = 13)
            .Build();

        firstOptions.ShouldNotBeNull();
        firstOptions.MaxDepth.ShouldBe(42);
        deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>().Options.ShouldNotBeSameAs(firstOptions);
    }

    [Fact]
    public void UseJsonSerializerContext_ShouldSetContextOptions()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .Configure(options => options.MaxDepth = 42)
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options.ShouldBeSameAs(TestEventOneJsonSerializerContext.Default.Options);
    }

    [Fact]
    public void Configure_ShouldApplyToContextOptions_WhenCalledAfterUseJsonSerializerContext()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Configure(options => options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default))
            .Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options.ShouldNotBeNull();
        jsonDeserializer.Options.TypeInfoResolverChain.ShouldBe(
        [
            TestEventOneJsonSerializerContext.Default,
            TestEventTwoJsonSerializerContext.Default
        ]);
        TestEventOneJsonSerializerContext.Default.Options.TypeInfoResolverChain.ShouldBe([TestEventOneJsonSerializerContext.Default]);
    }

    [Fact]
    public void Configure_ShouldKeepContextSettings_WhenCalledAfterUseJsonSerializerContext()
    {
        JsonMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder
            .UseJsonSerializerContext(CamelCaseTestEventOneJsonSerializerContext.Default)
            .Configure(options => options.MaxDepth = 42)
            .Build();

        JsonMessageDeserializer<object> jsonDeserializer = deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        jsonDeserializer.Options.ShouldNotBeNull();
        jsonDeserializer.Options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        jsonDeserializer.Options.MaxDepth.ShouldBe(42);
        jsonDeserializer.Options.TypeInfoResolver.ShouldBe(CamelCaseTestEventOneJsonSerializerContext.Default);
    }

    [Fact]
    public void Build_ShouldReturnEqualDeserializers_WhenSameJsonSerializerContexts()
    {
        IMessageDeserializer deserializer1 = new JsonMessageDeserializerBuilder()
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Configure(options => options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default))
            .Build();
        IMessageDeserializer deserializer2 = new JsonMessageDeserializerBuilder()
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Configure(options => options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default))
            .Build();

        deserializer1.ShouldBe(deserializer2);
    }

    [Fact]
    public void Build_ShouldReturnDifferentDeserializers_WhenDifferentJsonSerializerContexts()
    {
        IMessageDeserializer deserializer1 = new JsonMessageDeserializerBuilder()
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Build();
        IMessageDeserializer deserializer2 = new JsonMessageDeserializerBuilder()
            .UseJsonSerializerContext(TestEventTwoJsonSerializerContext.Default)
            .Build();

        deserializer1.ShouldNotBe(deserializer2);
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
