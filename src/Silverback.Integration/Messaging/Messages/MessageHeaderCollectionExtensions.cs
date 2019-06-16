// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    public static class MessageHeaderCollectionExtensions
    {
        public static T GetValue<T>(this IEnumerable<MessageHeader> headers, string headerName)
        {
            var value = headers.FirstOrDefault(h => h.Key == MessageHeader.FailedAttemptsHeaderName)?.Value;

            if (value == null)
                return default;

            try
            {
                return (T) Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
}