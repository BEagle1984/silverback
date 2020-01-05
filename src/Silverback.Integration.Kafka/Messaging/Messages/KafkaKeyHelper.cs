// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.HealthChecks;

#pragma warning disable 618 // Obsolete PartitioningKeyMemberAttribute

namespace Silverback.Messaging.Messages
{
    internal static class KafkaKeyHelper
    {
        public static string GetMessageKey(object message)
        {
            if (message is PingMessage)
                return Guid.NewGuid().ToString("N");
        
            var keysDictionary =
                message.GetType()
                    .GetProperties()
                    .Where(p => p.IsDefined(typeof(KafkaKeyMemberAttribute), true) ||
                                p.IsDefined(typeof(PartitioningKeyMemberAttribute), true))
                    .Select(p => new
                    {
                        p.Name,
                        Value = p.GetValue(message, null).ToString()
                    })
                    .ToList();

            if (!keysDictionary.Any())
                return null;

            return keysDictionary.Count == 1
                ? keysDictionary.First().Value
                : string.Join(",", keysDictionary.Select(p => $"{p.Name}={p.Value}"));
        }
    }
}