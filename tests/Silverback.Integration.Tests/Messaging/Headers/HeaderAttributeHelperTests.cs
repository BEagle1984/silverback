// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Headers
{
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public class HeaderAttributeHelperTests
    {
        [Fact]
        public void GetHeaders_DecoratedMessage_HeadersReturned()
        {
            var message = new TestEventWithHeaders
            {
                StringHeader = "string1",
                StringHeaderWithDefault = "string2",
                IntHeader = 1,
                IntHeaderWithDefault = 2
            };

            var result = HeaderAttributeHelper.GetHeaders(message);

            result.Should().BeEquivalentTo(
                new MessageHeader("x-string", "string1"),
                new MessageHeader("x-string-default", "string2"),
                new MessageHeader("x-readonly-string", "readonly"),
                new MessageHeader("x-int", "1"),
                new MessageHeader("x-int-default", "2"),
                new MessageHeader("x-readonly-int", "42"));
        }

        [Fact]
        public void GetHeaders_DecoratedPropertyPublishingDefaultValue_HeaderWithDefaultValueReturned()
        {
            var message = new TestEventWithHeaders();

            var result = HeaderAttributeHelper.GetHeaders(message);

            result.Should().ContainEquivalentOf(new MessageHeader("x-string-default", null));
            result.Should().ContainEquivalentOf(new MessageHeader("x-int-default", 0));
        }

        [Fact]
        public void GetHeaders_DecoratedPropertyWithoutPublishingDefaultValue_HeaderNotReturned()
        {
            var message = new TestEventWithHeaders();

            var result = HeaderAttributeHelper.GetHeaders(message);

            result.Select(header => header.Key).Should().NotContain("x-string");
            result.Select(header => header.Key).Should().NotContain("x-int");
        }

        [Fact]
        public void SetFromHeaders_DecoratedMessage_PropertiesSet()
        {
            var headers = new MessageHeaderCollection
            {
                { "x-string", "string1" },
                { "x-string-default", "string2" },
                { "x-readonly-string", "ignored" },
                { "x-int", "1" },
                { "x-int-default", "2" },
                { "x-readonly-int", "3" }
            };

            var message = new TestEventWithHeaders();

            HeaderAttributeHelper.SetFromHeaders(message, headers);

            message.StringHeader.Should().Be("string1");
            message.StringHeaderWithDefault.Should().Be("string2");
            message.ReadOnlyStringHeader.Should().Be("readonly");
            message.IntHeader.Should().Be(1);
            message.IntHeaderWithDefault.Should().Be(2);
            message.ReadOnlyIntHeader.Should().Be(42);
        }
    }
}