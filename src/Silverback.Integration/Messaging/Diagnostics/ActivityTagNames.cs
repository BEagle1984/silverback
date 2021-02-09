// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Contains the Tag Names of Tags added to the Activities.
    /// </summary>
    public static class ActivityTagNames
    {
        /// <summary>
        ///     Tag Name for the Message Identification. The Tag is written in the form [key]@[Value].
        /// </summary>
        public const string MessageId = "messaging.message_id";

        /// <summary>
        ///     Name for the Tag that contains the destination of the message (e.g. Endpoint Name).
        /// </summary>
        public const string MessageDestination = "messaging.destination";

        /// <summary>
        ///     Name for the tag that references the activity created for the sequence where this message has been added.
        /// </summary>
        public const string SequenceActivity = "messaging.sequence.activity";
    }
}
