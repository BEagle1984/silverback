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
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            if (baggageItems == null)
                throw new ArgumentNullException(nameof(baggageItems));

            foreach ((string key, string value) in baggageItems)
            {
                activity.AddBaggage(key, value);
            }
        }

        public static void SetMessageHeaders(this Activity activity, MessageHeaderCollection headers)
        {
            if (activity?.Id == null)
            {
                throw new InvalidOperationException(
                    "Activity.Id is null. Consider to start a new activity, before calling this method.");
            }

            headers.Add(new MessageHeader(DefaultMessageHeaders.TraceId, activity.Id));

            var traceState = activity.TraceStateString;
            if (traceState != null)
            {
                headers.Add(DefaultMessageHeaders.TraceState, traceState);
            }

            if (activity.Baggage.Any())
            {
                headers.Add(
                    new MessageHeader(
                        DefaultMessageHeaders.TraceBaggage,
                        ActivityBaggageSerializer.Serialize(activity.Baggage)));
            }
        }

        // See https://github.com/aspnet/AspNetCore/blob/master/src/Hosting/Hosting/src/Internal/HostingApplicationDiagnostics.cs
        public static void InitFromMessageHeaders(this Activity activity, MessageHeaderCollection headers)
        {
            string? traceId = headers.GetValue(DefaultMessageHeaders.TraceId);

            if (!string.IsNullOrEmpty(traceId))
            {
                // This will reflect, that the current activity is a child of the activity
                // which is represented in the message.
                activity.SetParentId(traceId);

                string? traceState = headers.GetValue(DefaultMessageHeaders.TraceState);
                if (!string.IsNullOrEmpty(traceState))
                {
                    activity.TraceStateString = traceState;
                }

                // The baggage is parsed last, so if it fails to be deserialized the rest is still setup.
                // We expect baggage to be empty by default.
                // Only very advanced users will be using it in near future, we encourage them to keep baggage small (few items).
                string? baggage = headers.GetValue(DefaultMessageHeaders.TraceBaggage);
                if (baggage != null)
                {
                    var baggageItems = ActivityBaggageSerializer.Deserialize(baggage);
                    AddBaggageRange(activity, baggageItems);
                }
            }
        }
    }
}
