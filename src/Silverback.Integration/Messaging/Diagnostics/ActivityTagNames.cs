// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Contains the name of tags added to the <see cref="Activity" />.
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
    ///     The name of the tag that contains the sequence identifier (e.g. the <see cref="BatchSequence"/> or <see cref="ChunkSequence"/> identifier).
    /// </summary>
    public const string SequenceId = "messaging.sequence_id";
}
