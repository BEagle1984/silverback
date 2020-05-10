// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Silverback.Util;

#pragma warning disable 618 //Obsolete

namespace Silverback.Messaging.Messages
{
    // TODO: Test + Cache?
    public class MessageIdProvider
    {
        private readonly IReadOnlyCollection<IMessageIdProvider> _providers;

        public MessageIdProvider(IEnumerable<IMessageIdProvider> providers)
        {
            _providers = providers.ToList();
        }

        public void EnsureMessageIdIsInitialized(object? message, MessageHeaderCollection headers)
        {
            Check.NotNull(headers, nameof(headers));

            string? messageKey = null;

            if (message != null)
            {
                messageKey = _providers.FirstOrDefault(
                    p =>
                        p.CanHandle(message))?.EnsureIdentifierIsInitialized(message);
            }

            if (!headers.Contains(DefaultMessageHeaders.MessageId))
            {
                headers.Add(
                    DefaultMessageHeaders.MessageId,
                    messageKey ?? Guid.NewGuid().ToString().ToLowerInvariant());
            }
        }
    }
}
