// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Confluent.Kafka;
using Silverback.Tools.Generators.Common;

namespace Silverback.Tools.Generators.KafkaConfigProxies;

public static class SerdeProperties
{
    // The settings declared in Config (e.g. CancellationDelayMaxMs) are not relevant for the serializers
    public static IEnumerable<PropertyInfo> Get(Type proxiedType) =>
        ReflectionHelper.GetProperties(proxiedType, true)
            .Where(property => property.DeclaringType != typeof(Config) && !IgnoredProperties.Contains(property));

    // The dictionaries are exposed as read-only and the non-nullable values as nullable to set only what is configured
    public static string GetTypeString(PropertyInfo property)
    {
        if (IsDictionary(property))
            return "System.Collections.Generic.IReadOnlyDictionary<string, string>?";

        if (IsNonNullableValueType(property))
            return ReflectionHelper.GetTypeString(typeof(Nullable<>).MakeGenericType(property.PropertyType));

        return ReflectionHelper.GetTypeString(property.PropertyType);
    }

    public static bool IsDictionary(PropertyInfo property) => property.PropertyType == typeof(IDictionary<string, string>);

    public static bool IsNonNullableValueType(PropertyInfo property) =>
        property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null;
}
