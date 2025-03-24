// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Reflection;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics;

internal static class ActivitySources
{
    private const string ProduceActivityName = "Silverback.Integration.Produce";

    private const string ConsumeActivityName = "Silverback.Integration.Consume";

    private static readonly ActivitySource ProduceActivitySource = new(ProduceActivityName);

    private static readonly ActivitySource ConsumeActivitySource = new(ConsumeActivityName);

    public static Activity StartProduceActivity(IOutboundEnvelope envelope)
    {
        Activity activity = ProduceActivitySource.ForceStartActivity(
            ProduceActivityName,
            ActivityKind.Producer,
            null,
            null);

        activity.SetEndpointName(envelope.EndpointConfiguration.RawName);
        activity.SetMessageHeaders(envelope.Headers);

        return activity;
    }

    public static Activity StartConsumeActivity(IRawInboundEnvelope envelope)
    {
        MessageHeaderCollection headers = envelope.Headers;
        string? traceIdFromHeader = headers.GetValue(DefaultMessageHeaders.TraceId);
        string? traceState = headers.GetValue(DefaultMessageHeaders.TraceState);

        Activity activity = ConsumeActivitySource.ForceStartActivity(
            ConsumeActivityName,
            ActivityKind.Consumer,
            traceIdFromHeader,
            traceState);

        activity.SetEndpointName(envelope.Endpoint.RawName);
        activity.AddBaggageFromHeaders(headers);

        return activity;
    }

    public static void UpdateConsumeActivity(IRawInboundEnvelope envelope)
    {
        Activity? currentActivity = Activity.Current;

        if (currentActivity == null)
            return;

        using Activity activity = StartConsumeActivity(envelope);

        FieldInfo? traceStateField = typeof(Activity).GetField("_traceState", BindingFlags.NonPublic | BindingFlags.Instance);
        traceStateField?.SetValue(currentActivity, activity.TraceStateString);
        FieldInfo? idField = typeof(Activity).GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance);
        idField?.SetValue(currentActivity, activity.Id);
        FieldInfo? rootIdField = typeof(Activity).GetField("_rootId", BindingFlags.NonPublic | BindingFlags.Instance);
        rootIdField?.SetValue(currentActivity, activity.RootId);
        FieldInfo? parentIdField = typeof(Activity).GetField("_parentId", BindingFlags.NonPublic | BindingFlags.Instance);
        parentIdField?.SetValue(currentActivity, activity.ParentId);
        FieldInfo? parentSpanIdField = typeof(Activity).GetField("_parentSpanId", BindingFlags.NonPublic | BindingFlags.Instance);
        parentSpanIdField?.SetValue(currentActivity, activity.ParentSpanId.ToString());
        FieldInfo? traceIdField = typeof(Activity).GetField("_traceId", BindingFlags.NonPublic | BindingFlags.Instance);
        traceIdField?.SetValue(currentActivity, activity.TraceId.ToString());
        FieldInfo? spanIdField = typeof(Activity).GetField("_spanId", BindingFlags.NonPublic | BindingFlags.Instance);
        spanIdField?.SetValue(currentActivity, activity.SpanId.ToString());

        currentActivity.SetEndpointName(envelope.Endpoint.RawName);
        currentActivity.ClearBaggage();
        currentActivity.AddBaggageFromHeaders(envelope.Headers);
    }

    private static Activity ForceStartActivity(
        this ActivitySource activitySource,
        string activityName,
        ActivityKind kind,
        string? traceIdFromHeader,
        string? traceState)
    {
        Activity? activity = activitySource.StartWithTraceId(activityName, kind, traceIdFromHeader, traceState);

        if (activity == null)
        {
            // No one is listening, but we still force the activity to be started (the activity information might be logged)
            activity = new Activity(activityName);

            if (traceIdFromHeader != null)
                activity.SetTraceIdAndState(traceIdFromHeader, traceState);

            activity.Start();
        }

        return activity;
    }

    private static void AddBaggageFromHeaders(this Activity activity, MessageHeaderCollection headers)
    {
        string? baggage = headers.GetValue(DefaultMessageHeaders.TraceBaggage);

        if (baggage == null)
            return;

        activity.AddBaggageRange(ActivityBaggageSerializer.Deserialize(baggage));
    }
}
