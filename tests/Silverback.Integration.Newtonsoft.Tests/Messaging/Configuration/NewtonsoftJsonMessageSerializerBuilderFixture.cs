// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Configuration;

public class NewtonsoftJsonMessageSerializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultSerializer()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.Build();

        serializer.Should().BeOfType<NewtonsoftJsonMessageSerializer>();
        serializer.As<NewtonsoftJsonMessageSerializer>().MustSetTypeHeader.Should().BeTrue();
    }

    [Fact]
    public void WithEncoding_ShouldSetEncoding()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.WithEncoding(MessageEncoding.UTF32).Build();

        serializer.As<NewtonsoftJsonMessageSerializer>().Encoding.Should().Be(MessageEncoding.UTF32);
    }

    [Fact]
    public void Configure_ShouldSetSettings()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder
            .Configure(
                settings =>
                {
                    settings.MaxDepth = 42;
                })
            .Build();

        serializer.As<NewtonsoftJsonMessageSerializer>().Settings.MaxDepth.Should().Be(42);
    }

    [Fact]
    public void SetTypeHeader_ShouldEnableTypeHeader()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.SetTypeHeader().Build();

        serializer.As<NewtonsoftJsonMessageSerializer>().MustSetTypeHeader.Should().BeTrue();
    }

    [Fact]
    public void DisableTypeHeader_ShouldDisableTypeHeader()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.DisableTypeHeader().Build();

        serializer.As<NewtonsoftJsonMessageSerializer>().MustSetTypeHeader.Should().BeFalse();
    }
}
