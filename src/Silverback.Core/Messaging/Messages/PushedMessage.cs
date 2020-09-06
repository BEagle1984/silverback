// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    internal class PushedMessage
    {
        public PushedMessage(int id, object? message, object? originalMessage)
        {
            Id = id;
            Message = Check.NotNull(message, nameof(message));
            OriginalMessage = Check.NotNull(originalMessage, nameof(originalMessage));
        }

        /// <summary>
        ///     Gets the message identifier. The identifier is unique for a given stream.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     Gets the message that is being enumerated.
        /// </summary>
        public object Message { get; }

        /// <summary>
        ///     Gets the original, unwrapped, untransformed message.
        /// </summary>
        public object OriginalMessage { get; }
    }
}
