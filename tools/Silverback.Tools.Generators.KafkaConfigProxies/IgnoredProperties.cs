// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Reflection;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public static class IgnoredProperties
{
    public static bool Contains(PropertyInfo property) => Contains(property.Name);

    private static bool Contains(string propertyName) =>
        propertyName
            is "EnableAutoOffsetStore" // the offsets will be explicitly stored only after successful processing
            or "EnableBackgroundPoll" // the background thread is needed
            or "DeliveryReportFields" // limited by default to key and status since no other value is used
            or "LogQueue"
            or "LogThreadName"
            or "EnableRandomSeed"
            or "LogConnectionClose"
            or "InternalTerminationSignal";
}
