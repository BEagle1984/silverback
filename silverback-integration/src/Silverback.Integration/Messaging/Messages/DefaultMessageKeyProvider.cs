// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;

namespace Silverback.Messaging.Messages
{
    public class DefaultPropertiesMessageKeyProvider : IMessageKeyProvider
    {
        public bool CanHandle(object message) => GetIdProperty(message) != null;

        public string GetKey(object message) => GetIdProperty(message)?.GetValue(message).ToString();

        public void EnsureKeyIsInitialized(object message)
        {
            var prop = GetIdProperty(message);

            if (!prop.CanRead || !prop.CanWrite)
                return;

            if (prop.PropertyType == typeof(Guid))
            {
                var current = prop.GetValue(message);

                if (current != null && (Guid)current != Guid.Empty)
                    return;

                prop.SetValue(message, Guid.NewGuid());
            }
            else if (prop.PropertyType == typeof(string))
            {
                var current = prop.GetValue(message);

                if (!string.IsNullOrWhiteSpace((string)current))
                    return;

                prop.SetValue(message, Guid.NewGuid().ToString());
            }
        }

        private PropertyInfo GetIdProperty(object message) =>
            message != null
                ? message.GetType().GetProperty("Id") ??
                  message.GetType().GetProperty("MessageId")
                : null;
    }
}