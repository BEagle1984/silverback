// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

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

        public string GetKey(object message, bool throwIfCannotGet = true)
        {
            if (message == null)
                return null;

            var provider = _providers.FirstOrDefault(p => p.CanHandle(message));

            if (provider == null)
                return throwIfCannotGet
                    ? throw new SilverbackException(
                        $"No IMessageIdProvider suitable for the message of type {message.GetType().FullName} " +
                        $"has been found. Consider registering an appropriate IMessageIdProvider implementation or add " +
                        $"an Id or MessageId property of type Guid or String to your messages.")
                    : (string) null;

            return provider.GetId(message);
        }

        public void EnsureKeyIsInitialized(object message, MessageHeaderCollection headers)
        {
            var key = _providers.FirstOrDefault(p =>
                p.CanHandle(message))?.EnsureIdentifierIsInitialized(message);

            headers.AddOrReplace(MessageHeader.MessageIdKey, key ?? Guid.NewGuid().ToString().ToLower());
        }
    }
}