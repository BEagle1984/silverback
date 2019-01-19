// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IMessageKeyProvider
    {
        bool CanHandle(object message);

        string GetKey(object message);

        void EnsureKeyIsInitialized(object message);
    }
}
