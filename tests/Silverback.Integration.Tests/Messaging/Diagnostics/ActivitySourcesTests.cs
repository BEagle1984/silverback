// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics
{
    public class ActivitySourcesTests
    {
        [Fact]
        public void StartConsumeActivity_WithoutActivityHeaders_ActivityIsNotModified()
        {
            var envelope = CreateInboundEnvelope(new MessageHeaderCollection());

            Activity activity = ActivitySources.StartConsumeActivity(envelope);

            activity.TraceStateString.Should().BeNull();
            activity.Baggage.Should().BeEmpty();
        }

        [Fact]
        public void StartConsumeActivity_WithoutStateAndBaggageHeaders_ActivityStateAndBaggageAreNotSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
            };

            var envelope = CreateInboundEnvelope(headers);
            Activity activity = ActivitySources.StartConsumeActivity(envelope);

            activity.TraceStateString.Should().BeNull();
            activity.Baggage.Should().BeEmpty();
        }

        [Fact]
        public void StartConsumeActivity_WithBaggageHeader_ActivityBaggageIsSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
                { DefaultMessageHeaders.TraceBaggage, "key1=value1" }
            };

            var envelope = CreateInboundEnvelope(headers);
            Activity activity = ActivitySources.StartConsumeActivity(envelope);

            activity.Baggage.Should().ContainEquivalentOf(new KeyValuePair<string, string>("key1", "value1"));
        }

        [Fact]
        public void StartConsumeActivity_WithStateHeader_ActivityStateIsSet()
        {
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
                { DefaultMessageHeaders.TraceState, "key1=value1" }
            };

            var envelope = CreateInboundEnvelope(headers);
            Activity activity = ActivitySources.StartConsumeActivity(envelope);

            activity.TraceStateString.Should().Be("key1=value1");
        }

        [Fact]
        public void StartConsumeActivity_WhenActivitySourceListener_StartsActivityThroughSource()
        {
            using var listener = new TestActivityListener();

            var envelope = CreateInboundEnvelope(new MessageHeaderCollection());
            Activity activity = ActivitySources.StartConsumeActivity(envelope);

            listener.Activites.Should().Contain(activity);
        }

        private static IRawInboundEnvelope CreateInboundEnvelope(MessageHeaderCollection headers)
        {
            return new RawInboundEnvelope(
                Stream.Null,
                headers,
                new TestConsumerEndpoint("Endpoint"),
                "Endpoint",
                new TestOffset("key", "7"));
        }

        private class TestActivityListener : IDisposable
        {
            private readonly ActivityListener _listener;

            private readonly List<Activity> _activites = new();

            public TestActivityListener()
            {
                _listener = new ActivityListener();
                _listener.ShouldListenTo = s => true;
                _listener.ActivityStarted = a => _activites.Add(a);
                _listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded;
                ActivitySource.AddActivityListener(_listener);
            }

            public IEnumerable<Activity> Activites => _activites;

            public void Dispose()
            {
                _listener.Dispose();
            }
        }
    }
}
