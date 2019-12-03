// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    internal static class MessageHeaderExtensions
    {
        public static string GetFromHeaders(this IEnumerable<MessageHeader> headers, string key)
        {
            var header = headers?.FirstOrDefault(h => h.Key == key);
            string value = null;
            if (header != null && !string.IsNullOrWhiteSpace(header.Value))
            {
                value = header.Value;
            }

            return value;
        }
    }
}