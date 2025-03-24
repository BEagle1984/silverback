// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
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

        JsonMessageSerializer jsonSerializer = serializer.ShouldBeOfType<JsonMessageSerializer>();
        jsonSerializer.MustSetTypeHeader.ShouldBeTrue();
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
        serializer.Options.MaxDepth.ShouldBe(42);
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
