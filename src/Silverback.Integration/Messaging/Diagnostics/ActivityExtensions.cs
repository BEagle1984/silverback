// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Diagnostics;

internal static class ActivityExtensions
{
    public static void AddBaggageRange(
        this Activity activity,
        IEnumerable<KeyValuePair<string, string>> baggageItems)
    {
        Check.NotNull(activity, nameof(activity));
        Check.NotNull(baggageItems, nameof(baggageItems));

        foreach ((string key, string value) in baggageItems)
        {
            activity.AddBaggage(key, value);
        }
    }

    public static void SetMessageHeaders(this Activity activity, MessageHeaderCollection headers)
    {
        if (activity.Id == null)
        {
            throw new InvalidOperationException("Activity.Id is null. Consider to start a new activity, before calling this method.");
        }

        headers.Add(DefaultMessageHeaders.TraceId, activity.Id);

        string? traceState = activity.TraceStateString;
        if (traceState != null)
        {
            headers.Add(DefaultMessageHeaders.TraceState, traceState);
        }

        if (activity.Baggage.Any())
        {
            headers.Add(
                DefaultMessageHeaders.TraceBaggage,
                ActivityBaggageSerializer.Serialize(activity.Baggage));
        }
    }

    public static void SetTraceIdAndState(this Activity activity, string? traceId, string? traceState)
    {
        if (!string.IsNullOrEmpty(traceId))
        {
            activity.SetParentId(traceId);

            if (!string.IsNullOrEmpty(traceState))
            {
                activity.TraceStateString = traceState;
            }
        }
    }

    public static void AddEndpointName(this Activity activity, string endpointName)
    {
        activity.SetTag(ActivityTagNames.MessageDestination, endpointName);
    }

    public static Activity? StartWithTraceId(
        this ActivitySource activitySource,
        string name,
        ActivityKind activityKind,
        string? traceId,
        string? traceState)
    {
        if (!activitySource.HasListeners())
        {
            return null;
        }

        if (traceId != null && ActivityContext.TryParse(traceId, traceState, out ActivityContext context))
        {
            return activitySource.StartActivity(name, activityKind, context);
        }

        return activitySource.StartActivity(name, activityKind);
    }
}
