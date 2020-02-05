// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.HealthChecks;

namespace Silverback.Messaging.Messages
{
    internal static class RoutingKeyHelper
    {
        public static string GetRoutingKey(object message)
        {
            if (message is PingMessage)
                return "ping";

            var messageType = message.GetType();

            var keys =
                messageType
                    .GetProperties()
                    .Where(p => p.IsDefined(typeof(RabbitRoutingKeyAttribute), true))
                    .Select(p => p.GetValue(message, null).ToString())
                    .ToList();

            if (!keys.Any())
                return null;

            if (keys.Count > 1)
                throw new InvalidOperationException(
                    "Multiple properties are decorated with RabbitRoutingKeyAttribute " +
                    $"in type {messageType.Name}.");

            return keys.First();
        }
    }
}