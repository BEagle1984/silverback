﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class ActivityExtensionsTests
    {
        public ActivityExtensionsTests()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        }

        [Fact]
        public void AddBaggageRange_SomeNewItems_ItemsAreAppended()
        {
            var activity = new Activity("test");
            IList<KeyValuePair<string, string>> itemsToAdd = new List<KeyValuePair<string, string>>
            {
                new("key1", "value1"),
                new("key2", "value2"),
                new("key3", "value3")
            };

            activity.AddBaggage("key0", "value0");

            activity.AddBaggageRange(itemsToAdd);

            activity.Baggage.Should().BeEquivalentTo(
                new[]
                {
                    new KeyValuePair<string, string>("key0", "value0"),
                    new KeyValuePair<string, string>("key1", "value1"),
                    new KeyValuePair<string, string>("key2", "value2"),
                    new KeyValuePair<string, string>("key3", "value3")
                });
        }

        [Fact]
        public void SetMessageHeaders_StartedActivity_TraceIdHeaderIsSet()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.Start();
            activity.SetMessageHeaders(headers);

            headers.Should()
                .ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.TraceId, activity.Id));
        }

        [Fact]
        public void SetMessageHeaders_ActivityWithState_TraceStateHeaderIsSet()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.Start();
            activity.TraceStateString = "Test";
            activity.SetMessageHeaders(headers);

            headers.Should().ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.TraceState, "Test"));
        }

        [Fact]
        public void SetMessageHeaders_ActivityWithoutState_TraceStateHeaderIsNotSet()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.Start();
            activity.SetMessageHeaders(headers);

            headers.Should().NotContain(h => h.Name == DefaultMessageHeaders.TraceState);
        }

        [Fact]
        public void SetMessageHeaders_ActivityWithBaggage_BaggageHeaderIsSet()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.Start();
            activity.AddBaggage("key1", "value1");
            activity.SetMessageHeaders(headers);

            headers.Should().ContainEquivalentOf(
                new MessageHeader(DefaultMessageHeaders.TraceBaggage, "key1=value1"));
        }

        [Fact]
        public void SetMessageHeaders_ActivityWithoutBaggage_BaggageHeaderIsNotSet()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.Start();
            activity.SetMessageHeaders(headers);

            headers.Should().NotContain(h => h.Name == DefaultMessageHeaders.TraceBaggage);
        }

        [Fact]
        public void SetTraceIdAndState_WithTraceIdHeader_ActivityParentIdIsSet()
        {
            var activity = new Activity("test");
            activity.SetTraceIdAndState("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", "state=1");
            activity.Start();

            activity.ParentId.Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c");
            activity.TraceStateString.Should().Be("state=1");
        }

        [Fact]
        public void AddEndpointName_ActivityTagIsAdded()
        {
            var activity = new Activity("test");
            activity.AddEndpointName("MyEndpoint");

            activity.Tags.Should().ContainSingle(
                kv => kv.Key == ActivityTagNames.MessageDestination && kv.Value == "MyEndpoint");
        }
    }
}
