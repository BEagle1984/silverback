// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.Diagnostics
{
    public class ActivityExtensionsTests
    {
        public ActivityExtensionsTests()
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        }

        [Fact]
        public void AddBaggageRange_AddsItemsToExistingItems()
        {
            var activity = new Activity("test");
            IList<KeyValuePair<string, string>> itemsToAdd = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("key1", "value1"),
                new KeyValuePair<string, string>("key2", "value2"),
                new KeyValuePair<string, string>("key3", "value3")
            };

            activity.AddBaggage("key0", "value0");

            activity.AddBaggageRange(itemsToAdd);

            Assert.Contains(activity.Baggage, pair => pair.Key == "key0" && pair.Value == "value0");
            Assert.Contains(activity.Baggage,
                pair => pair.Key == itemsToAdd[0].Key && pair.Value == itemsToAdd[0].Value);
            Assert.Contains(activity.Baggage,
                pair => pair.Key == itemsToAdd[1].Key && pair.Value == itemsToAdd[1].Value);
            Assert.Contains(activity.Baggage,
                pair => pair.Key == itemsToAdd[2].Key && pair.Value == itemsToAdd[2].Value);
        }

        [Fact]
        public void FillActivityIBrokerMessage_FillsTraceId()
        {
            var message = new OutboundMessage(null, Enumerable.Empty<MessageHeader>(), Substitute.For<IEndpoint>());

            var activity = new Activity("test");
            activity.Start();
            activity.FillActivity(message);

            message.Headers.Should().Contain(h => h.Key == DiagnosticsConstants.TraceIdHeaderKey && h.Value == activity.Id);
        }

        [Fact]
        public void FillActivityIBrokerMessage_FillsTraceStateString_IfNotNull()
        {
            var message = new OutboundMessage(null, Enumerable.Empty<MessageHeader>(), Substitute.For<IEndpoint>());

            var activity = new Activity("test");
            activity.Start();
            activity.TraceStateString = "Test";
            activity.FillActivity(message);

            message.Headers.Should().Contain(h => h.Key == DiagnosticsConstants.TraceStateHeaderKey && h.Value == "Test");
        }

        [Fact]
        public void FillActivityIBrokerMessage_DoesNotFillTraceStateString_IfNull()
        {
            var message = new OutboundMessage(null, Enumerable.Empty<MessageHeader>(), Substitute.For<IEndpoint>());

            var activity = new Activity("test");
            activity.Start();
            activity.FillActivity(message);

            message.Headers.Should().NotContain(h => h.Key == DiagnosticsConstants.TraceStateHeaderKey);
        }

        [Fact]
        public void FillActivityIBrokerMessage_FillsBaggage_IfNotNull()
        {
            var message = new OutboundMessage(null, Enumerable.Empty<MessageHeader>(), Substitute.For<IEndpoint>());

            var activity = new Activity("test");
            activity.Start();
            activity.AddBaggage("key1", "value1");
            activity.FillActivity(message);

            message.Headers.Should().Contain(h => h.Key == DiagnosticsConstants.TraceBaggageHeaderKey && h.Value == "key1=value1");
        }

        [Fact]
        public void FillActivityIBrokerMessage_DoesNotFillBaggage_IfNull()
        {
            var message = new OutboundMessage(null, Enumerable.Empty<MessageHeader>(), Substitute.For<IEndpoint>());

            var activity = new Activity("test");
            activity.Start();
            activity.FillActivity(message);

            message.Headers.Should().NotContain(h => h.Key == DiagnosticsConstants.TraceBaggageHeaderKey);
        }

        [Fact]
        public void FillActivityMessageHeader_FillsTraceId()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader(DiagnosticsConstants.TraceIdHeaderKey,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")
            };

            var activity = new Activity("test");
            activity.FillActivity(messageHeaders);
            activity.Start();

            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
        }

        [Fact]
        public void FillActivityMessageHeader_DoesNotFillTraceStateStringNorBaggage_IfNoActivityIdHeaderIsPresent()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>();

            var activity = new Activity("test");
            activity.FillActivity(messageHeaders);
            activity.Start();

            activity.TraceStateString.Should().BeNull();
            activity.Baggage.Should().BeEmpty();
        }

        [Fact]
        public void FillActivityMessageHeader_DoesNotFillTraceState_IfNotPresent()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader(DiagnosticsConstants.TraceIdHeaderKey,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")
            };

            var activity = new Activity("test");
            activity.FillActivity(messageHeaders);
            activity.Start();

            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
            activity.TraceStateString.Should().BeNull();
        }

        [Fact]
        public void FillActivityMessageHeader_DoesNotFillBaggage_IfNotPresent()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader(DiagnosticsConstants.TraceIdHeaderKey,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")
            };

            var activity = new Activity("test");
            activity.FillActivity(messageHeaders);
            activity.Start();

            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
            activity.Baggage.Should().BeEmpty();
        }
        
        [Fact]
        public void FillActivityMessageHeader_FillBaggage_IfPresent()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader(DiagnosticsConstants.TraceIdHeaderKey,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"),
                new MessageHeader(DiagnosticsConstants.TraceBaggageHeaderKey,
                    "key1=value1")
            };

            var activity = new Activity("test");
            activity.FillActivity(messageHeaders);
            activity.Start();

            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
            activity.Baggage.Should().Contain(b => b.Key == "key1" && b.Value == "value1");
        }


        [Fact]
        public void FillActivityMessageHeader_FillTraceState_IfPresent()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader(DiagnosticsConstants.TraceIdHeaderKey,
                    "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"),
                new MessageHeader(DiagnosticsConstants.TraceStateHeaderKey,
                    "key1=value1")
            };

            var activity = new Activity("test");
            activity.FillActivity(messageHeaders);
            activity.Start();

            activity.Id.Should().StartWith("00-0af7651916cd43dd8448eb211c80319c-");
            activity.TraceStateString.Should().Be("key1=value1");
        }
    }
}