// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal static class MessageIdProvider
{
    public static void EnsureMessageIdIsInitialized(MessageHeaderCollection headers)
    {
        Check.NotNull(headers, nameof(headers));

        if (!headers.Contains(DefaultMessageHeaders.MessageId))
        {
            headers.Add(DefaultMessageHeaders.MessageId, Guid.NewGuid().ToString());
        }
    }
}
