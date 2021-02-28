// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represents a tombstone message (a message with null body).
    /// </summary>
    public class Tombstone
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Tombstone" /> class.
        /// </summary>
        /// <param name="messageId">
        ///     The message identifier.
        /// </param>
        public Tombstone(string? messageId)
        {
            MessageId = messageId;
        }

        /// <summary>
        ///     Gets the message identifier.
        /// </summary>
        [Header(DefaultMessageHeaders.MessageId)]
        public string? MessageId { get; }
    }
}
