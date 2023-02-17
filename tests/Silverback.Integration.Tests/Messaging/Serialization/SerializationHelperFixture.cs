// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

// Note: the SerializationHelper is also implicitly tested by the serializer tests
public class SerializationHelperFixture
{
    [Fact]
    public void GetTypeFromHeader_ShouldReturnExistingType()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                typeof(ChildClass).AssemblyQualifiedName
            }
        };

        Type? type = SerializationHelper.GetTypeFromHeaders(headers);

        type.Should().Be(typeof(ChildClass));
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnExistingType_WhenSpecifyingBaseType()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                typeof(ChildClass).AssemblyQualifiedName
            }
        };

        Type type = SerializationHelper.GetTypeFromHeaders(headers, typeof(BaseClass));

        type.Should().Be(typeof(ChildClass));
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnExistingType_WhenAssemblyVersionIsWrong()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration, Version=123.123.123.123"
            }
        };

        Type? type = SerializationHelper.GetTypeFromHeaders(headers);

        type.Should().NotBeNull();
        type!.AssemblyQualifiedName.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnExistingType_WhenAssemblyVersionIsWrongAndSpecifyingBaseType()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration, Version=123.123.123.123"
            }
        };

        Type type = SerializationHelper.GetTypeFromHeaders(headers, typeof(object));

        type.AssemblyQualifiedName.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
    }

    [Fact]
    public void GetTypeFromHeader_ShouldThrow_WhenTypeDoesNotExist()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Baaaad.Event, Silverback.Integration.Tests"
            }
        };

        Action act = () => SerializationHelper.GetTypeFromHeaders(headers);

        act.Should().Throw<TypeLoadException>();
    }

    [Fact]
    public void GetTypeFromHeader_ShouldThrow_WhenTypeDoesNotExistAndSpecifyingBaseType()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Baaaad.Event, Silverback.Integration.Tests"
            }
        };

        Action act = () => SerializationHelper.GetTypeFromHeaders(headers, typeof(object));

        act.Should().Throw<TypeLoadException>();
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnType_WhenNameIsIncomplete()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration"
            }
        };

        Type? type = SerializationHelper.GetTypeFromHeaders(headers);

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnType_WhenNameIsIncompleteAndSpecifyingBaseType()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration"
            }
        };

        Type type = SerializationHelper.GetTypeFromHeaders(headers, typeof(object));

        type.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnNull_WhenHeadersAreEmpty()
    {
        Type? type = SerializationHelper.GetTypeFromHeaders(new MessageHeaderCollection());

        type.Should().BeNull();
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnBaseType_WhenHeadersAreEmpty()
    {
        Type type = SerializationHelper.GetTypeFromHeaders(new MessageHeaderCollection(), typeof(object));

        type.Should().Be(typeof(object));
    }

    [Fact]
    public void GetTypeFromHeader_ShouldReturnBaseType_WhenHeadersTypeIsNotSubclass()
    {
        MessageHeaderCollection headers = new()
        {
            {
                "x-message-type",
                typeof(BaseClass).AssemblyQualifiedName
            }
        };

        Type type = SerializationHelper.GetTypeFromHeaders(headers, typeof(ChildClass));

        type.Should().Be(typeof(ChildClass));
    }

    [Fact]
    public void CreateTypedInboundEnvelope_ShouldReturnEnvelope()
    {
        TestConsumerEndpoint endpoint = TestConsumerEndpoint.GetDefault();
        RawInboundEnvelope rawEnvelope = new(
            Array.Empty<byte>(),
            new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") },
            endpoint,
            Substitute.For<IConsumer>(),
            new TestOffset());
        TestEventOne message = new();

        IRawInboundEnvelope envelope = SerializationHelper.CreateTypedInboundEnvelope(rawEnvelope, message, typeof(TestEventOne));

        envelope.Should().BeOfType<InboundEnvelope<TestEventOne>>();
        envelope.As<InboundEnvelope<TestEventOne>>().Message.Should().Be(message);
        envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        envelope.Endpoint.Should().Be(endpoint);
    }

    private class BaseClass
    {
    }

    private class ChildClass : BaseClass
    {
    }
}
