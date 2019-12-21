// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    // TODO: Test
    public static class MessageHeaderCollectionExtensions
    {
        public static bool Contains(this IEnumerable<MessageHeader> headers, string key) =>
            headers.Any(h => h.Key == key);

        public static string GetValue(this IEnumerable<MessageHeader> headers, string key) => 
            headers.FirstOrDefault(h => h.Key == key)?.Value;

        public static T? GetValue<T>(this IEnumerable<MessageHeader> headers, string key)
            where T : struct
        {
            var value = headers.FirstOrDefault(h => h.Key == key)?.Value;

            if (value == null)
                return null;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return null;
            }
        }

        public static T GetValueOrDefault<T>(this IEnumerable<MessageHeader> headers, string key)
            where T : struct =>
            GetValue<T>(headers, key) ?? default;
    }
}