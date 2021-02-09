// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics
{
    internal static class ActivitySources
    {
        private const string ProduceActivityName = "Silverback.Integration.Produce";

        private const string ConsumeActivityName = "Silverback.Integration.Consume";

        private const string SequenceActivityName = "Silverback.Integration.Sequence";

        private static readonly ActivitySource ProduceActivitySource = new(ProduceActivityName);

        private static readonly ActivitySource ConsumeActivitySource = new(ConsumeActivityName);

        public static Activity StartProduceActivity(IRawOutboundEnvelope envelope)
        {
            Activity activity = ProduceActivitySource.ForceStartActivity(
                ProduceActivityName,
                ActivityKind.Producer,
                null,
                null);

            activity.AddEndpointName(envelope.ActualEndpointName);
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

            activity.AddEndpointName(envelope.ActualEndpointName);
            activity.AddBaggageFromHeaders(headers);

            return activity;
        }

        public static Activity? StartSequenceActivity()
        {
            Activity? messageActivity = Activity.Current;
            Activity? sequenceAcivity = ConsumeActivitySource.StartActivity(
                SequenceActivityName,
                ActivityKind.Internal,
                new ActivityContext(
                    ActivityTraceId.CreateRandom(),
                    ActivitySpanId.CreateRandom(),
                    ActivityTraceFlags.None));

            if (messageActivity != null && sequenceAcivity?.Id != null)
            {
                messageActivity.SetTag(ActivityTagNames.SequenceActivity, sequenceAcivity.Id);
            }

            return sequenceAcivity;
        }

        private static Activity ForceStartActivity(
            this ActivitySource activitySource,
            string activityName,
            ActivityKind kind,
            string? traceIdFromHeader,
            string? traceState)
        {
            Activity? activity = activitySource.StartWithTraceId(
                activityName,
                kind,
                traceIdFromHeader,
                traceState);

            if (activity == null)
            {
                // No one is listening to this activity. We just set the correct trace info to make sure
                // subsequent activities contain that information as well.
                activity = new Activity(activityName);

                if (traceIdFromHeader != null)
                {
                    activity.SetTraceIdAndState(traceIdFromHeader, traceState);
                }

                activity.Start();
            }

            return activity;
        }

        private static void AddBaggageFromHeaders(this Activity activity, MessageHeaderCollection headers)
        {
            string? baggage = headers.GetValue(DefaultMessageHeaders.TraceBaggage);
            if (baggage != null)
            {
                IReadOnlyCollection<KeyValuePair<string, string>> baggageItems =
                    ActivityBaggageSerializer.Deserialize(baggage);

                activity.AddBaggageRange(baggageItems);
            }
        }
    }
}
