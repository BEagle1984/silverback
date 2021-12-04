// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Headers;

public class HeaderAttributeHelperTests
{
    [Fact]
    public void GetHeaders_DecoratedMessage_HeadersReturned()
    {
        TestEventWithHeaders message = new()
        {
            StringHeader = "string1",
            StringHeaderWithDefault = "string2",
            IntHeader = 1,
            IntHeaderWithDefault = 2
        };

        IEnumerable<MessageHeader> result = HeaderAttributeHelper.GetHeaders(message);

        result.Should().BeEquivalentTo(
            new[]
            {
                new MessageHeader("x-string", "string1"),
                new MessageHeader("x-string-default", "string2"),
                new MessageHeader("x-readonly-string", "readonly"),
                new MessageHeader("x-int", "1"),
                new MessageHeader("x-int-default", "2"),
                new MessageHeader("x-readonly-int", "42")
            });
    }

    [Fact]
    public void GetHeaders_DecoratedPropertyPublishingDefaultValue_HeaderWithDefaultValueReturned()
    {
        TestEventWithHeaders message = new();

        List<MessageHeader> result = HeaderAttributeHelper.GetHeaders(message).ToList();

        result.Should().ContainEquivalentOf(new MessageHeader("x-string-default", null));
        result.Should().ContainEquivalentOf(new MessageHeader("x-int-default", "0"));
    }

    [Fact]
    public void GetHeaders_DecoratedPropertyWithoutPublishingDefaultValue_HeaderNotReturned()
    {
        TestEventWithHeaders message = new();

        List<MessageHeader> result = HeaderAttributeHelper.GetHeaders(message).ToList();

        result.Select(header => header.Name).Should().NotContain("x-string");
        result.Select(header => header.Name).Should().NotContain("x-int");
    }

    [Fact]
    public void SetFromHeaders_DecoratedMessage_PropertiesSet()
    {
        MessageHeaderCollection headers = new()
        {
            { "x-string", "string1" },
            { "x-string-default", "string2" },
            { "x-readonly-string", "ignored" },
            { "x-int", "1" },
            { "x-int-default", "2" },
            { "x-readonly-int", "3" }
        };

        TestEventWithHeaders message = new();

        HeaderAttributeHelper.SetFromHeaders(message, headers);

        message.StringHeader.Should().Be("string1");
        message.StringHeaderWithDefault.Should().Be("string2");
        message.ReadOnlyStringHeader.Should().Be("readonly");
        message.IntHeader.Should().Be(1);
        message.IntHeaderWithDefault.Should().Be(2);
        message.ReadOnlyIntHeader.Should().Be(42);
    }
}
