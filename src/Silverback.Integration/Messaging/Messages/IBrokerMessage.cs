// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IBrokerMessage : IRawBrokerMessage
    {
        /// <summary>
        /// Gets the deserialized message body.
        /// </summary>
        object Content { get; }
    }
}