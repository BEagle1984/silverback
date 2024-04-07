// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class JsonMessageSerializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultSerializer()
    {
        JsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.Build();

        serializer.Should().BeOfType<JsonMessageSerializer>();
        serializer.As<JsonMessageSerializer>().MustSetTypeHeader.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldSetOptions()
    {
        JsonMessageSerializerBuilder builder = new();

        JsonMessageSerializer serializer = (JsonMessageSerializer)builder
            .Configure(
                options =>
                {
                    options.MaxDepth = 42;
                })
            .Build();

        serializer.Options.ShouldNotBeNull();
        serializer.Options.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void SetTypeHeader_ShouldEnableTypeHeader()
    {
        JsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.SetTypeHeader().Build();

        serializer.As<JsonMessageSerializer>().MustSetTypeHeader.Should().BeTrue();
    }

    [Fact]
    public void DisableTypeHeader_ShouldDisableTypeHeader()
    {
        JsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.DisableTypeHeader().Build();

        serializer.As<JsonMessageSerializer>().MustSetTypeHeader.Should().BeFalse();
    }
}
