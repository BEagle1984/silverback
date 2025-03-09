// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Diagnostics;

public class ActivitySourcesFixture
{
    [Fact]
    public void StartConsumeActivity_ShouldStartActivity()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.ShouldBeSameAs(activity);
        Activity.Current.TraceId.ToString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        Activity.Current.TraceStateString.ShouldBe(null);
    }

    [Fact]
    public void StartConsumeActivity_ShouldSetTraceIdAndState()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { DefaultMessageHeaders.TraceState, "my-state" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        ActivitySources.StartConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.TraceId.ToString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        Activity.Current.TraceStateString.ShouldBe("my-state");
    }

    [Fact]
    public void StartConsumeActivity_ShouldStartActivity_WhenHeadersNotSet()
    {
        IRawInboundEnvelope envelope = CreateInboundEnvelope([]);

        ActivitySources.StartConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.TraceId.ToString().ShouldNotBeNullOrEmpty();
        Activity.Current.TraceStateString.ShouldBeNull();
        Activity.Current.Baggage.ShouldBeEmpty();
    }

    [Fact]
    public void StartConsumeActivity_ShouldAddBaggage()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { DefaultMessageHeaders.TraceBaggage, "key1=value1" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        ActivitySources.StartConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.Baggage.ShouldContain(new KeyValuePair<string, string?>("key1", "value1"));
    }

    [Fact]
    public void StartConsumeActivity_ShouldSetMessageDestinationTag()
    {
        IRawInboundEnvelope envelope = CreateInboundEnvelope([]);
        ActivitySources.StartConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.Tags.ShouldContain(new KeyValuePair<string, string?>(ActivityTagNames.MessageDestination, "Endpoint"));
    }

    [Fact]
    public void StartConsumeActivity_ShouldStartsActivityThroughSource_WhenListenerIsSetup()
    {
        using TestActivityListener listener = new();

        IRawInboundEnvelope envelope = CreateInboundEnvelope([]);
        Activity activity = ActivitySources.StartConsumeActivity(envelope);

        listener.Activities.ShouldContain(activity);
        Activity.Current.ShouldNotBeNull();
        Activity.Current.ShouldBeSameAs(activity);
    }

    [Fact]
    public void UpdateConsumeActivity_ShouldSetTraceIdOnCurrentActivity()
    {
        using Activity activity = new("activity");
        activity.Start();

        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        ActivitySources.UpdateConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.TraceId.ToString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        Activity.Current.TraceStateString.ShouldBe(null);
    }

    [Fact]
    public void UpdateConsumeActivity_ShouldSetTraceIdAndStateOnCurrentActivity()
    {
        using Activity activity = new("activity");
        activity.Start();

        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { DefaultMessageHeaders.TraceState, "my-state" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        ActivitySources.UpdateConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.TraceId.ToString().ShouldBe("0af7651916cd43dd8448eb211c80319c");
        Activity.Current.TraceStateString.ShouldBe("my-state");
    }

    [Fact]
    public void UpdateConsumeActivity_ShouldNotModifyCurrentActivity_WhenHeadersNotSet()
    {
        using Activity activity = new("activity");
        activity.Start();
        string traceId = activity.TraceId.ToString();

        IRawInboundEnvelope envelope = CreateInboundEnvelope([]);
        ActivitySources.UpdateConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.TraceId.ToString().ShouldBe(traceId);
        Activity.Current.TraceStateString.ShouldBeNull();
        Activity.Current.Baggage.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateConsumeActivity_ShouldReplaceBaggageOnCurrentActivity()
    {
        using Activity activity = new("activity");
        activity.AddBaggage("another-key", "another-value");
        activity.Start();

        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { DefaultMessageHeaders.TraceBaggage, "key1=value1" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        ActivitySources.UpdateConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.Baggage.Count().ShouldBe(1);
        Activity.Current.Baggage.ShouldContain(new KeyValuePair<string, string?>("key1", "value1"));
    }

    [Fact]
    public void UpdateConsumeActivity_ShouldSetMessageDestinationTag()
    {
        using Activity activity = new("activity");
        activity.Start();

        IRawInboundEnvelope envelope = CreateInboundEnvelope([]);
        ActivitySources.UpdateConsumeActivity(envelope);

        Activity.Current.ShouldNotBeNull();
        Activity.Current.Tags.ShouldContain(new KeyValuePair<string, string?>(ActivityTagNames.MessageDestination, "Endpoint"));
    }

    [Fact]
    public void UpdateConsumeActivity_ShouldDoNothing_WhenNoActivityIsRunning()
    {
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.TraceId, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" }
        };

        IRawInboundEnvelope envelope = CreateInboundEnvelope(headers);
        ActivitySources.UpdateConsumeActivity(envelope);

        Activity.Current.ShouldBeNull();
    }

    private static IRawInboundEnvelope CreateInboundEnvelope(MessageHeaderCollection headers) =>
        new RawInboundEnvelope(
            Stream.Null,
            headers,
            new TestConsumerEndpointConfiguration("Endpoint").GetDefaultEndpoint(),
            Substitute.For<IConsumer>(),
            new TestOffset("key", "7"));

    private sealed class TestActivityListener : IDisposable
    {
        private readonly ActivityListener _listener;

        private readonly List<Activity> _activities = [];

        public TestActivityListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                ActivityStarted = _activities.Add,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public IEnumerable<Activity> Activities => _activities;

        public void Dispose() => _listener.Dispose();
    }
}
