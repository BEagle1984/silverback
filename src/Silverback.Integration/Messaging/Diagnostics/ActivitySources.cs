// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics;

internal static class ActivitySources
{
    private const string ProduceActivityName = "Silverback.Integration.Produce";

    private const string ConsumeActivityName = "Silverback.Integration.Consume";

    private static readonly ActivitySource ProduceActivitySource = new(ProduceActivityName);

    private static readonly ActivitySource ConsumeActivitySource = new(ConsumeActivityName);

    // Returns the ambient produce activity (started by the outbox worker when producing outside the pipeline), ignoring any other activity
    // (e.g. the caller's own one). The operation name is checked, since the activity forcibly started when no one is listening doesn't
    // belong to the source.
    public static Activity? GetCurrentProduceActivity() =>
        Activity.Current is { OperationName: ProduceActivityName } activity ? activity : null;

    // Activity.Stop sets the current activity of the calling thread to the one that was current when the activity was started, which
    // pollutes the thread when the activity is stopped from another thread (e.g. the one delivering the broker acknowledgment)
    public static void StopPreservingCurrent(this Activity activity)
    {
        Activity? current = Activity.Current;

        activity.Stop();

        RestoreCurrent(current);
    }

    // Restores the activity that was current before another one was set. Activity.Current ignores a stopped activity and would keep the
    // other one current, so no activity is set instead (e.g. on a broker thread whose execution context captured an activity that ended).
    public static void RestoreCurrent(Activity? previousActivity) =>
        Activity.Current = previousActivity is { IsStopped: true } ? null : previousActivity;

    public static Activity StartProduceActivity(IOutboundEnvelope envelope) =>
        StartProduceActivity(envelope.Headers, envelope.EndpointConfiguration.RawName);

    public static Activity StartProduceActivity(MessageHeaderCollection headers, string endpointName)
    {
        // The headers already contain the trace context when producing a message stored in the outbox
        string? traceIdFromHeader = headers.GetValue(DefaultMessageHeaders.TraceId);
        string? traceState = headers.GetValue(DefaultMessageHeaders.TraceState);

        Activity activity = ProduceActivitySource.ForceStartActivity(
            ProduceActivityName,
            ActivityKind.Producer,
            traceIdFromHeader,
            traceState);

        if (traceIdFromHeader != null)
            activity.AddBaggageFromHeaders(headers);

        activity.SetEndpointName(endpointName);
        activity.SetMessageHeaders(headers);

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

    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "The private fields of Activity are the only way to make the ambient activity continue the one created from the message headers")]
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
        PropertyInfo? hasRemoteParentProperty = typeof(Activity).GetProperty(nameof(Activity.HasRemoteParent));
        hasRemoteParentProperty?.SetValue(currentActivity, activity.HasRemoteParent);

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
