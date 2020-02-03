// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IEnvelope
    {
        /// <summary>
        ///     Gets the message body.
        /// </summary>
        object Message { get; }
    }
}