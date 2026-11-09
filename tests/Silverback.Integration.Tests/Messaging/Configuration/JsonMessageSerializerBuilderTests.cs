// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class JsonMessageSerializerBuilderTests
{
    [Fact]
    public void Build_ShouldReturnDefaultSerializer()
    {
        JsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.Build();

        JsonMessageSerializer jsonSerializer = serializer.ShouldBeOfType<JsonMessageSerializer>();
        jsonSerializer.MustSetTypeHeader.ShouldBeTrue();
    }

    [Fact]
    public void Configure_ShouldSetOptions()
    {
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
            .Configure(options =>
            {
                options.MaxDepth = 42;
            })
            .Build();

        serializer.Options.ShouldNotBeNull();
        serializer.Options.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void Configure_ShouldApplyToPreviousOptions_WhenCalledMultipleTimes()
    {
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
            .Configure(options => options.MaxDepth = 42)
            .Configure(options => options.AllowTrailingCommas = true)
            .Build();

        serializer.Options.ShouldNotBeNull();
        serializer.Options.MaxDepth.ShouldBe(42);
        serializer.Options.AllowTrailingCommas.ShouldBeTrue();
    }

    [Fact]
    public void Configure_ShouldNotAlterPreviousOptions()
    {
        JsonSerializerOptions? firstOptions = null;
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
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
        serializer.Options.ShouldNotBeSameAs(firstOptions);
    }

    [Fact]
    public void UseJsonSerializerContext_ShouldSetContextOptions()
    {
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
            .Configure(options => options.MaxDepth = 42)
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Build();

        serializer.Options.ShouldBeSameAs(TestEventOneJsonSerializerContext.Default.Options);
    }

    [Fact]
    public void Configure_ShouldApplyToContextOptions_WhenCalledAfterUseJsonSerializerContext()
    {
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Configure(options => options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default))
            .Build();

        serializer.Options.ShouldNotBeNull();
        serializer.Options.TypeInfoResolverChain.ShouldBe(
        [
            TestEventOneJsonSerializerContext.Default,
            TestEventTwoJsonSerializerContext.Default
        ]);
        TestEventOneJsonSerializerContext.Default.Options.TypeInfoResolverChain.ShouldBe([TestEventOneJsonSerializerContext.Default]);
    }

    [Fact]
    public void Configure_ShouldKeepContextSettings_WhenCalledAfterUseJsonSerializerContext()
    {
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
            .UseJsonSerializerContext(CamelCaseTestEventOneJsonSerializerContext.Default)
            .Configure(options => options.MaxDepth = 42)
            .Build();

        serializer.Options.ShouldNotBeNull();
        serializer.Options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        serializer.Options.MaxDepth.ShouldBe(42);
        serializer.Options.TypeInfoResolver.ShouldBe(CamelCaseTestEventOneJsonSerializerContext.Default);
    }

    [Fact]
    public void Build_ShouldReturnEqualSerializers_WhenSameJsonSerializerContexts()
    {
        IMessageSerializer serializer1 = new JsonMessageSerializerBuilder()
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Configure(options => options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default))
            .Build();
        IMessageSerializer serializer2 = new JsonMessageSerializerBuilder()
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Configure(options => options.TypeInfoResolverChain.Add(TestEventTwoJsonSerializerContext.Default))
            .Build();

        serializer1.ShouldBe(serializer2);
    }

    [Fact]
    public void Build_ShouldReturnDifferentSerializers_WhenDifferentJsonSerializerContexts()
    {
        IMessageSerializer serializer1 = new JsonMessageSerializerBuilder()
            .UseJsonSerializerContext(TestEventOneJsonSerializerContext.Default)
            .Build();
        IMessageSerializer serializer2 = new JsonMessageSerializerBuilder()
            .UseJsonSerializerContext(TestEventTwoJsonSerializerContext.Default)
            .Build();

        serializer1.ShouldNotBe(serializer2);
    }

    [Fact]
    public void SetTypeHeader_ShouldEnableTypeHeader()
    {
        JsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.SetTypeHeader().Build();

        JsonMessageSerializer jsonSerializer = serializer.ShouldBeOfType<JsonMessageSerializer>();
        jsonSerializer.MustSetTypeHeader.ShouldBeTrue();
    }

    [Fact]
    public void DisableTypeHeader_ShouldDisableTypeHeader()
    {
        JsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.DisableTypeHeader().Build();

        JsonMessageSerializer jsonSerializer = serializer.ShouldBeOfType<JsonMessageSerializer>();
        jsonSerializer.MustSetTypeHeader.ShouldBeFalse();
    }
}
