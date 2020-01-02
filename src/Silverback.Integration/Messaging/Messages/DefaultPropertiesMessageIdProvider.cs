// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;

namespace Silverback.Messaging.Messages
{
    public class DefaultPropertiesMessageIdProvider : IMessageIdProvider
    {
        public bool CanHandle(object message)
        {
            var idProperty = GetIdProperty(message);

            return idProperty != null &&
                   (idProperty.PropertyType == typeof(Guid) || idProperty.PropertyType == typeof(string));
        }

        public string GetId(object message) => GetIdProperty(message)?.GetValue(message).ToString();

        public string EnsureIdentifierIsInitialized(object message)
        {
            var prop = GetIdProperty(message);

            if (!prop.CanRead || !prop.CanWrite)
                throw new InvalidOperationException("The id property must be a read/write property.");

            if (prop.PropertyType == typeof(Guid))
            {
                var current = prop.GetValue(message);

                if (current != null && (Guid) current != Guid.Empty)
                    return current.ToString().ToLower();

                var newValue = Guid.NewGuid();
                prop.SetValue(message, newValue);
                return newValue.ToString();
            }
            else if (prop.PropertyType == typeof(string))
            {
                var current = (string) prop.GetValue(message);

                if (!string.IsNullOrWhiteSpace(current))
                    return current;

                var newValue = Guid.NewGuid().ToString().ToLower();
                prop.SetValue(message, newValue);
                return newValue;
            }

            throw new InvalidOperationException("Unhandled id property type.");
        }

        private PropertyInfo GetIdProperty(object message) =>
            message != null
                ? message.GetType().GetProperty("Id") ??
                  message.GetType().GetProperty("MessageId")
                : null;
    }
}