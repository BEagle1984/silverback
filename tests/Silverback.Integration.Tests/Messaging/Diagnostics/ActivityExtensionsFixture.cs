// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using Shouldly;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivityExtensionsFixture
{
    public ActivityExtensionsFixture()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    }

    [Fact]
    public void AddBaggageRange_ShouldAppendItems()
    {
        Activity activity = new("test");
        IList<KeyValuePair<string, string>> itemsToAdd =
        [
            new("key1", "value1"),
            new("key2", "value2"),
            new("key3", "value3")
        ];

        activity.AddBaggage("key0", "value0");

        activity.AddBaggageRange(itemsToAdd);

        activity.Baggage.ShouldBe(
            [
                new KeyValuePair<string, string?>("key0", "value0"),
                new KeyValuePair<string, string?>("key1", "value1"),
                new KeyValuePair<string, string?>("key2", "value2"),
                new KeyValuePair<string, string?>("key3", "value3")
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void ClearBaggage_ShouldRemoveAllItems()
    {
        Activity activity = new("test");
        activity.AddBaggage("key1", "value1");
        activity.AddBaggage("key2", "value2");

        activity.ClearBaggage();

        activity.Baggage.ShouldBeEmpty();
    }

    [Fact]
    public void SetMessageHeaders_ShouldSetTraceIdHeader()
    {
        MessageHeaderCollection headers = [];

        Activity activity = new("test");
        activity.Start();
        activity.SetMessageHeaders(headers);

        headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.TraceId, activity.Id));
    }

    [Fact]
    public void SetMessageHeaders_ShouldSetTraceStateHeader()
    {
        MessageHeaderCollection headers = [];

        Activity activity = new("test");
        activity.Start();
        activity.TraceStateString = "Test";
        activity.SetMessageHeaders(headers);

        headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.TraceState, "Test"));
    }

    [Fact]
    public void SetMessageHeaders_ShouldNotSetTraceStateHeader_WhenTraceStateNotSet()
    {
        MessageHeaderCollection headers = [];

        Activity activity = new("test");
        activity.Start();
        activity.SetMessageHeaders(headers);

        headers.ShouldNotContain(header => header.Name == DefaultMessageHeaders.TraceState);
    }

    [Fact]
    public void SetMessageHeaders_ShouldSetBaggageHeader()
    {
        MessageHeaderCollection headers = [];

        Activity activity = new("test");
        activity.Start();
        activity.AddBaggage("key1", "value1");
        activity.SetMessageHeaders(headers);

        headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.TraceBaggage, "key1=value1"));
    }

    [Fact]
    public void SetMessageHeaders_ShouldNotSetBaggageHeader_WhenBaggageNotSet()
    {
        MessageHeaderCollection headers = [];

        Activity activity = new("test");
        activity.Start();
        activity.SetMessageHeaders(headers);

        headers.ShouldNotContain(h => h.Name == DefaultMessageHeaders.TraceBaggage);
    }

    [Fact]
    public void SetTraceIdAndState_ShouldSetActivityParentId()
    {
        Activity activity = new("test");
        activity.SetTraceIdAndState("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", "state=1");
        activity.Start();

        activity.ParentId.ShouldBe("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        activity.TraceId.ToString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        activity.Id.ShouldStartWith("00-0af7651916cd43dd8448eb211c80319c");
        activity.TraceStateString.ShouldBe("state=1");
    }

    [Fact]
    public void SetEndpointName_ShouldAddDestinationTag()
    {
        Activity activity = new("test");
        activity.SetEndpointName("MyEndpoint");

        activity.Tags.ShouldContain(pair => pair.Key == ActivityTagNames.MessageDestination && pair.Value == "MyEndpoint");
    }
}
