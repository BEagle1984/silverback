// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Headers
{
    internal static class HeaderAttributeHelper
    {
        private static readonly ConcurrentDictionary<string, IReadOnlyCollection<DecoratedProperty>> PropertiesCache =
            new();

        public static IEnumerable<MessageHeader> GetHeaders(object? message)
        {
            if (message == null)
                yield break;

            var type = message.GetType();

            var properties = GetDecoratedProperties(type)
                .Where(property => property.PropertyInfo.CanRead)
                .ToList();

            if (!properties.Any())
                yield break;

            foreach (var property in properties)
            {
                var value = property.PropertyInfo.GetValue(message);

                if (value != null && !value.Equals(property.PropertyInfo.PropertyType.GetDefaultValue()) ||
                    property.Attribute.PublishDefaultValue)
                {
                    yield return new MessageHeader(property.Attribute.HeaderName, value);
                }
            }
        }

        public static void SetFromHeaders(object? message, MessageHeaderCollection headers)
        {
            if (message == null)
                return;

            var type = message.GetType();

            var properties = GetDecoratedProperties(type)
                .Where(property => property.PropertyInfo.CanWrite)
                .ToList();

            if (!properties.Any())
                return;

            foreach (var property in properties)
            {
                property.PropertyInfo.SetValue(
                    message,
                    headers.GetValueOrDefault(property.Attribute.HeaderName, property.PropertyInfo.PropertyType));
            }
        }

        private static IReadOnlyCollection<DecoratedProperty> GetDecoratedProperties(Type type) =>
            PropertiesCache.GetOrAdd(
                type.Name,
                _ =>
                    type.GetProperties()
                        .Select(
                            propertyInfo =>
                                new DecoratedProperty(
                                    propertyInfo,
                                    propertyInfo.GetCustomAttribute<HeaderAttribute>(true)))
                        .Where(decoratedProperty => decoratedProperty.Attribute != null)
                        .ToList());

        private class DecoratedProperty
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
}
