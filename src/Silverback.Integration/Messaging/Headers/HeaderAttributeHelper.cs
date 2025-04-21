// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers;

internal static class HeaderAttributeHelper
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<DecoratedProperty>> PropertiesCache = new();

    public static IEnumerable<MessageHeader> GetHeaders(object? message)
    {
        if (message == null)
            yield break;

        Type type = message.GetType();

        List<DecoratedProperty> properties = GetDecoratedProperties(type)
            .Where(property => property.PropertyInfo.CanRead)
            .ToList();

        if (properties.Count == 0)
            yield break;

        foreach (DecoratedProperty property in properties)
        {
            object? value = property.PropertyInfo.GetValue(message);

            if (value != null && !value.Equals(property.PropertyInfo.PropertyType.GetDefaultValue()) ||
                property.Attribute.PublishDefaultValue)
            {
                yield return new MessageHeader(property.Attribute.HeaderName, value?.ToString());
            }
        }
    }

    public static void SetFromHeaders(object? message, MessageHeaderCollection headers)
    {
        if (message == null)
            return;

        Type type = message.GetType();

        List<DecoratedProperty> properties = GetDecoratedProperties(type)
            .Where(property => property.PropertyInfo.CanWrite)
            .ToList();

        if (properties.Count == 0)
            return;

        foreach (DecoratedProperty property in properties)
        {
            property.PropertyInfo.SetValue(
                message,
                headers.GetValueOrDefault(property.Attribute.HeaderName, property.PropertyInfo.PropertyType));
        }
    }

    private static IReadOnlyCollection<DecoratedProperty> GetDecoratedProperties(Type type) =>
        PropertiesCache.GetOrAdd(
            type,
            static key =>
                key.GetProperties()
                    .Select(propertyInfo => new DecoratedProperty(propertyInfo, propertyInfo.GetCustomAttribute<HeaderAttribute>(true)!))
                    .Where(decoratedProperty => decoratedProperty.Attribute != null)
                    .ToList());

    private sealed class DecoratedProperty
    {
        public DecoratedProperty(PropertyInfo propertyInfo, HeaderAttribute attribute)
        {
            PropertyInfo = propertyInfo;
            Attribute = attribute;
        }

        public PropertyInfo PropertyInfo { get; }

        public HeaderAttribute Attribute { get; }
    }
}
