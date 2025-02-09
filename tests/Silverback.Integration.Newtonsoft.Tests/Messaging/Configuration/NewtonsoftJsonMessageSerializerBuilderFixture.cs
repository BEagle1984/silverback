// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
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

        serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
        serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>().MustSetTypeHeader.ShouldBeTrue();
    }

    [Fact]
    public void WithEncoding_ShouldSetEncoding()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.WithEncoding(MessageEncoding.UTF32).Build();

        serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>().Encoding.ShouldBe(MessageEncoding.UTF32);
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

        NewtonsoftJsonMessageSerializer newtonsoftJsonMessageSerializer = serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>();
        newtonsoftJsonMessageSerializer.Settings.ShouldNotBeNull();
        newtonsoftJsonMessageSerializer.Settings.MaxDepth.ShouldBe(42);
    }

    [Fact]
    public void SetTypeHeader_ShouldEnableTypeHeader()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.SetTypeHeader().Build();

        serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>().MustSetTypeHeader.ShouldBeTrue();
    }

    [Fact]
    public void DisableTypeHeader_ShouldDisableTypeHeader()
    {
        NewtonsoftJsonMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.DisableTypeHeader().Build();

        serializer.ShouldBeOfType<NewtonsoftJsonMessageSerializer>().MustSetTypeHeader.ShouldBeFalse();
    }
}
