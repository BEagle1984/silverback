// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization
{
    // Note: the SerializationHelper is also implicitly tested by the serializer tests
    public class SerializationHelperTests
    {
        [Fact]
        public void GetTypeFromHeader_ExistingType_TypeReturned()
        {
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    typeof(TestEventOne).AssemblyQualifiedName
                }
            };

            var type = SerializationHelper.GetTypeFromHeaders(headers);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void GetTypeFromHeader_WrongAssemblyVersion_TypeReturned()
        {
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration, Version=123.123.123.123"
                }
            };

            var type = SerializationHelper.GetTypeFromHeaders(headers);

            type.Should().NotBeNull();
            type!.AssemblyQualifiedName.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
        }

        [Fact]
        public void GetTypeFromHeader_NonExistingType_ExceptionThrown()
        {
            var headers = new MessageHeaderCollection
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
        public void GetTypeFromHeader_IncompleteTypeName_TypeReturned()
        {
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration"
                }
            };

            var type = SerializationHelper.GetTypeFromHeaders(headers);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void GetTypeFromHeader_NoHeader_NullReturned()
        {
            var type = SerializationHelper.GetTypeFromHeaders(new MessageHeaderCollection());

            type.Should().BeNull();
        }

        [Fact]
        public void CreateTypedInboundEnvelope_EnvelopeReturned()
        {
            var endpoint = TestConsumerEndpoint.GetDefault();
            var rawEnvelope = new RawInboundEnvelope(
                Array.Empty<byte>(),
                new[] { new MessageHeader("one", "1"), new MessageHeader("two", "2") },
                endpoint,
                "test",
                new TestOffset());
            var message = new TestEventOne();

            var envelope = SerializationHelper.CreateTypedInboundEnvelope(rawEnvelope, message, typeof(TestEventOne));

            envelope.Should().BeOfType<InboundEnvelope<TestEventOne>>();
            envelope.As<InboundEnvelope<TestEventOne>>().Message.Should().Be(message);
            envelope.Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
            envelope.Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
            envelope.Endpoint.Should().Be(endpoint);
        }
    }
}
