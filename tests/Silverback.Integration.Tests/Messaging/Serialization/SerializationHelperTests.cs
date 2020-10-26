// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
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
                    "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests, Version=123.123.123.123"
                }
            };

            var type = SerializationHelper.GetTypeFromHeaders(headers);

            type.AssemblyQualifiedName.Should().Be(typeof(TestEventOne).AssemblyQualifiedName);
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
        public void GetTypeFromHeader_NonExistingTypeWithNoThrow_NullReturned()
        {
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Baaaad.Event, Silverback.Integration.Tests"
                }
            };

            var type = SerializationHelper.GetTypeFromHeaders(headers, false);

            type.Should().BeNull();
        }

        [Fact]
        public void GetTypeFromHeader_IncompleteTypeName_TypeReturned()
        {
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests"
                }
            };

            var type = SerializationHelper.GetTypeFromHeaders(headers);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void GetTypeFromHeader_NoHeaderNullReturned()
        {
            var headers = new MessageHeaderCollection();

            var type = SerializationHelper.GetTypeFromHeaders(headers);

            type.Should().BeNull();
        }
    }
}
