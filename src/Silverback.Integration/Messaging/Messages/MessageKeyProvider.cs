// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    // TODO: Test + Cache?
    public class MessageKeyProvider
    {
        private readonly IEnumerable<IMessageKeyProvider> _providers;

        public MessageKeyProvider(IEnumerable<IMessageKeyProvider> providers)
        {
            _providers = providers;
        }

        public string GetKey(object message, bool throwIfCannotGet = true)
        {
            if (message == null)
                return null;

            var provider = _providers.FirstOrDefault(p => p.CanHandle(message));

            if (provider == null)
                return throwIfCannotGet
                    ? throw new SilverbackException($"No IMessageKeyProvider suitable for the message of type {message.GetType().FullName} has been provided.")
                    : (string)null;

            return provider.GetKey(message);
        }

        public void EnsureKeyIsInitialized(object message) =>
            _providers.FirstOrDefault(p => p.CanHandle(message))?.EnsureKeyIsInitialized(message);
    }
}