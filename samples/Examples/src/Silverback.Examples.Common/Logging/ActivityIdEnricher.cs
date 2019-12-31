// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Silverback.Examples.Common.Logging
{
    public class ActivityIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ActivityId", Activity.Current?.Id));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentActivityId",
                Activity.Current?.ParentId));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ActivityRootId", Activity.Current?.RootId));
        }
    }
}