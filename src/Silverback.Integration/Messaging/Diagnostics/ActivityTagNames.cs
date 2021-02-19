// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Contains the name of tags added to the <see cref="Activity"/>.
    /// </summary>
    public static class ActivityTagNames
    {
        /// <summary>
        ///     The name of the tag whose value identifies the message.
        /// </summary>
        /// <remarks>
        ///     For Kafka the tag value will be in the form topic[partition]@offset.
        /// </remarks>
        public const string MessageId = "messaging.message_id";

        /// <summary>
        ///     The name of the tag that contains the destination of the message (i.e. the name of the endpoint).
        /// </summary>
        public const string MessageDestination = "messaging.destination";

        /// <summary>
        ///     The name of the tag that references the activity created for the sequence where this message has been
        ///     added.
        /// </summary>
        public const string SequenceActivity = "messaging.sequence.activity";
    }
}
