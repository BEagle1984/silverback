// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Shouldly;
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

        result.ShouldBe(
            [
                new MessageHeader("x-string", "string1"),
                new MessageHeader("x-string-default", "string2"),
                new MessageHeader("x-readonly-string", "readonly"),
                new MessageHeader("x-int", "1"),
                new MessageHeader("x-int-default", "2"),
                new MessageHeader("x-readonly-int", "42")
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void GetHeaders_DecoratedPropertyPublishingDefaultValue_HeaderWithDefaultValueReturned()
    {
        TestEventWithHeaders message = new();

        List<MessageHeader> result = HeaderAttributeHelper.GetHeaders(message).ToList();

        result.ShouldContain(new MessageHeader("x-string-default", null));
        result.ShouldContain(new MessageHeader("x-int-default", "0"));
    }

    [Fact]
    public void GetHeaders_DecoratedPropertyWithoutPublishingDefaultValue_HeaderNotReturned()
    {
        TestEventWithHeaders message = new();

        List<MessageHeader> result = HeaderAttributeHelper.GetHeaders(message).ToList();

        result.Select(header => header.Name).ShouldNotContain("x-string");
        result.Select(header => header.Name).ShouldNotContain("x-int");
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

        message.StringHeader.ShouldBe("string1");
        message.StringHeaderWithDefault.ShouldBe("string2");
        message.ReadOnlyStringHeader.ShouldBe("readonly");
        message.IntHeader.ShouldBe(1);
        message.IntHeaderWithDefault.ShouldBe(2);
        message.ReadOnlyIntHeader.ShouldBe(42);
    }
}
