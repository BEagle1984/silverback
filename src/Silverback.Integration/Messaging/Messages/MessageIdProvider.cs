// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

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

        public string GetMessageId(MessageHeaderCollection headers) =>
            headers.GetValue(DefaultMessageHeaders.MessageId) ??
            throw new InvalidOperationException($"No {DefaultMessageHeaders.MessageId} header was found.");

        public void EnsureMessageIdIsInitialized(object message, MessageHeaderCollection headers)
        {
            var key = _providers.FirstOrDefault(p =>
                p.CanHandle(message))?.EnsureIdentifierIsInitialized(message);

            if (!headers.Contains(DefaultMessageHeaders.MessageId))
                headers.Add(DefaultMessageHeaders.MessageId, key ?? Guid.NewGuid().ToString().ToLower());
        }
    }
}