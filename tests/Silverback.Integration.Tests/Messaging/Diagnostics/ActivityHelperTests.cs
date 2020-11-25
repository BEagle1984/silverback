// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class ActivityHelperTests
    {
        public ActivityHelperTests()
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
                new KeyValuePair<string, string>("key0", "value0"),
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3"));
        }

        [Fact]
        public void SetMessageHeaders_StartedActivity_TraceIdHeaderIsSet()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.Start();
            activity.SetMessageHeaders(headers);

            headers.Should().ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.TraceId, activity.Id));
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

            headers.Should().ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.TraceBaggage, "key1=value1"));
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
        public void SetMessageHeaders_NoCurrentActivity_ExceptionIsThrown()
        {
            var headers = new MessageHeaderCollection();

            Action act = () => Activity.Current.SetMessageHeaders(headers);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void InitFromMessageHeaders_WithTraceIdHeader_ActivityParentIdIsSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
            };

            var activity = new Activity("test");
            activity.InitFromMessageHeaders(headers);
            activity.Start();

            activity.ParentId.Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c");
        }

        [Fact]
        public void InitFromMessageHeaders_WithoutActivityHeaders_ActivityIsNotModified()
        {
            var headers = new MessageHeaderCollection();

            var activity = new Activity("test");
            activity.InitFromMessageHeaders(headers);
            activity.Start();

            activity.TraceStateString.Should().BeNull();
            activity.Baggage.Should().BeEmpty();
        }

        [Fact]
        public void InitFromMessageHeaders_WithoutStateAndBaggageHeaders_ActivityStateAndBaggageAreNotSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
            };

            var activity = new Activity("test");
            activity.InitFromMessageHeaders(headers);
            activity.Start();

            activity.TraceStateString.Should().BeNull();
            activity.Baggage.Should().BeEmpty();
        }

        [Fact]
        public void InitFromMessageHeaders_WithBaggageHeader_ActivityBaggageIsSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
                { DefaultMessageHeaders.TraceBaggage, "key1=value1" }
            };

            var activity = new Activity("test");
            activity.InitFromMessageHeaders(headers);
            activity.Start();

            activity.Baggage.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key1", "value1"));
        }

        [Fact]
        public void InitFromMessageHeaders_WithStateHeader_ActivityStateIsSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
                { DefaultMessageHeaders.TraceState, "key1=value1" }
            };

            var activity = new Activity("test");
            activity.InitFromMessageHeaders(headers);
            activity.Start();

            activity.TraceStateString.Should().Be("key1=value1");
        }
    }
}
