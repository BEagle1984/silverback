// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;

#pragma warning disable 618

namespace Silverback.Messaging.Messages
{
    internal static class KafkaKeyHelper
    {
        public static string GetMessageKey(object message)
        {
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
                .ToArray();

            if (!keysDictionary.Any())
                return null;

            return keysDictionary.Length == 1 
                ? keysDictionary.First().Value : 
                string.Join(",", keysDictionary.Select(p => $"{p.Name}={p.Value}"));
        }
    }
}