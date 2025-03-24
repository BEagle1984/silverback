// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Silverback.Diagnostics;

internal static class ActivitySources
{
    private static readonly ActivitySource SubscriberActivitySource = new("Silverback.Core.Subscribers");

    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument", Justification = "Reviewed")]
    public static Activity? StartInvokeSubscriberActivity(MethodInfo subscriberMethodInfo)
    {
        Activity? activity = SubscriberActivitySource.StartActivity("Silverback.Core.Subscribers.InvokeSubscriber");

        if (activity is { IsAllDataRequested: true })
        {
            activity.AddTag("SubscriberType", subscriberMethodInfo.DeclaringType?.Name ?? "global");
            activity.AddTag("SubscriberMethod", subscriberMethodInfo.Name);
        }

        return activity;
    }
}
