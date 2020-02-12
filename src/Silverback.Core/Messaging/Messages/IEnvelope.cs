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

        /// <summary>
        ///     Gets a boolean value indicating whether this envelope can be automatically unwrapped and the
        ///     message contained message can be forwarded to the matching subscribers in its pure form.
        /// </summary>
        bool AutoUnwrap { get; }
    }
}