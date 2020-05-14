// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

#pragma warning disable 618 //Obsolete

namespace Silverback.Messaging.Messages
{
    internal static class MessageIdProvider
    {
        public static void EnsureMessageIdIsInitialized(object? message, MessageHeaderCollection headers)
        {
            Check.NotNull(headers, nameof(headers));

            if (!headers.Contains(DefaultMessageHeaders.MessageId))
            {
                headers.Add(DefaultMessageHeaders.MessageId, Guid.NewGuid().ToString().ToUpperInvariant());
            }
        }
    }
}
