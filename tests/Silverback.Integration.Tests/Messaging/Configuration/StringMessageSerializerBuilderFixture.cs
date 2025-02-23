// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class StringMessageSerializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultSerializer()
    {
        StringMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.Build();

        StringMessageSerializer stringSerializer = serializer.ShouldBeOfType<StringMessageSerializer>();
        stringSerializer.Encoding.ShouldBe(MessageEncoding.UTF8);
    }

    [Fact]
    public void WithEncoding_ShouldSetEncoding()
    {
        StringMessageSerializerBuilder builder = new();

        IMessageSerializer serializer = builder.WithEncoding(MessageEncoding.ASCII).Build();

        StringMessageSerializer stringSerializer = serializer.ShouldBeOfType<StringMessageSerializer>();
        stringSerializer.Encoding.ShouldBe(MessageEncoding.ASCII);
    }
}
