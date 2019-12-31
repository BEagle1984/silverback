// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Diagnostics
{
    internal static class ActivityExtensions
    {
        public static void AddBaggageRange(
            this Activity activity,
            IEnumerable<KeyValuePair<string, string>> baggageItems)
        {
            foreach (var baggageItem in baggageItems)
            {
                activity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }
        }

        public static void SetMessageHeaders(this Activity activity, MessageHeaderCollection headers)
        {
            if (activity?.Id == null)
                throw new InvalidOperationException(
                    "Activity.Id is null. Consider to start a new activity, before calling this method.");

            headers.Add(new MessageHeader(DiagnosticsConstants.TraceIdHeaderKey, activity.Id));

            var traceState = activity.TraceStateString;
            if (traceState != null)
            {
                headers.Add(DiagnosticsConstants.TraceStateHeaderKey, traceState);
            }

            if (activity.Baggage.Any())
            {
                headers.Add(new MessageHeader(DiagnosticsConstants.TraceBaggageHeaderKey,
                    BaggageConverter.Serialize(activity.Baggage)));
            }
        }

        // See https://github.com/aspnet/AspNetCore/blob/master/src/Hosting/Hosting/src/Internal/HostingApplicationDiagnostics.cs
        public static void InitFromMessageHeaders(this Activity activity, MessageHeaderCollection headers)
        {
            var traceId = headers.GetValue(DiagnosticsConstants.TraceIdHeaderKey);
            if (!string.IsNullOrEmpty(traceId))
            {
                // This will reflect, that the current activity is a child of the activity
                // which is represented in the message.
                activity.SetParentId(traceId);

                var traceState = headers.GetValue(DiagnosticsConstants.TraceStateHeaderKey);
                if (!string.IsNullOrEmpty(traceState))
                {
                    activity.TraceStateString = traceState;
                }

                // We expect baggage to be empty by default.
                // Only very advanced users will be using it in near future, we encourage them to keep baggage small (few items).
                var baggage = headers.GetValue(DiagnosticsConstants.TraceBaggageHeaderKey);
                if (BaggageConverter.TryDeserialize(baggage, out var baggageItems))
                {
                    activity.AddBaggageRange(baggageItems);
                }
            }
        }
    }
}