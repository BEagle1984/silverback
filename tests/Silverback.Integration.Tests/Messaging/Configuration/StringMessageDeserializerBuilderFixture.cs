// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class StringMessageDeserializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultDeserializer()
    {
        StringMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.Build();

        StringMessageDeserializer<StringMessage> stringDeserializer = deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage>>();
        stringDeserializer.Encoding.ShouldBe(MessageEncoding.UTF8);
    }

    [Fact]
    public void UseDiscriminator_ShouldSetSerializerType()
    {
        StringMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseDiscriminator<TestEventOne>().Build();

        deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage<TestEventOne>>>();
    }

    [Fact]
    public void UseDiscriminator_ShouldSetSerializerType_WhenPassingType()
    {
        StringMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseDiscriminator(typeof(TestEventOne)).Build();

        deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage<TestEventOne>>>();
    }

    [Fact]
    public void WithEncoding_ShouldSetEncoding()
    {
        StringMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.WithEncoding(MessageEncoding.ASCII).Build();

        StringMessageDeserializer<StringMessage> stringDeserializer = deserializer.ShouldBeOfType<StringMessageDeserializer<StringMessage>>();
        stringDeserializer.Encoding.ShouldBe(MessageEncoding.ASCII);
    }
}
