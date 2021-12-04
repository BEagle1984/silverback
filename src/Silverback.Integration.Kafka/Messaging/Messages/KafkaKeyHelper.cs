// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Silverback.Messaging.Messages;

internal static class KafkaKeyHelper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TypeMessageKeyPropertyInfoCache =
        new();

    public static string? GetMessageKey(object? message)
    {
        if (message == null)
            return null;

        PropertyInfo[] propertyInfos = GetPropertyInfos(message);

        if (propertyInfos.Length == 0)
        {
            return null;
        }

        if (propertyInfos.Length == 1)
        {
            string? value = propertyInfos[0].GetValue(message, null)?.ToString();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        StringBuilder stringBuilder = BuildMessageKey(message, propertyInfos);
        return stringBuilder.Length == 0 ? null : stringBuilder.ToString();
    }

    private static PropertyInfo[] GetPropertyInfos(object message)
    {
        if (TypeMessageKeyPropertyInfoCache.TryGetValue(message.GetType(), out PropertyInfo[]? propertyInfos))
        {
            return propertyInfos;
        }

        Type messageType = message.GetType();
        propertyInfos = messageType
            .GetProperties()
            .Where(static propertyInfo => propertyInfo.IsDefined(typeof(KafkaKeyMemberAttribute), true))
            .ToArray();

        // Must not fail if there is already an entry for this type
        _ = TypeMessageKeyPropertyInfoCache.TryAdd(messageType, propertyInfos);

        return propertyInfos;
    }

    private static StringBuilder BuildMessageKey(
        object? message,
        IReadOnlyList<PropertyInfo> propertyInfos)
    {
        StringBuilder stringBuilder = new();
        for (int i = 0; i < propertyInfos.Count; i++)
        {
            string name = propertyInfos[i].Name;
            string? value = propertyInfos[i].GetValue(message, null)?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(',');
            }

            stringBuilder.Append(name);
            stringBuilder.Append('=');
            stringBuilder.Append(value);
        }

        return stringBuilder;
    }
}
