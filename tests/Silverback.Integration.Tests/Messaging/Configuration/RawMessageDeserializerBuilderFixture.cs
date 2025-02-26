// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class RawMessageDeserializerBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnDefaultDeserializer()
    {
        RawMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.Build();

        deserializer.ShouldBeOfType<RawMessageDeserializer<RawMessage>>();
    }

    [Fact]
    public void UseDiscriminator_ShouldSetSerializerType()
    {
        RawMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseDiscriminator<TestEventOne>().Build();

        deserializer.ShouldBeOfType<RawMessageDeserializer<RawMessage<TestEventOne>>>();
    }

    [Fact]
    public void UseDiscriminator_ShouldSetSerializerType_WhenPassingType()
    {
        RawMessageDeserializerBuilder builder = new();

        IMessageDeserializer deserializer = builder.UseDiscriminator(typeof(TestEventOne)).Build();

        deserializer.ShouldBeOfType<RawMessageDeserializer<RawMessage<TestEventOne>>>();
    }
}
