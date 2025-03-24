// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Sequences;

/// <summary>
///     The possible reasons for a sequence to be aborted.
/// </summary>
public enum SequenceAbortReason
{
    /// <summary>
    ///     The sequence isn't aborted.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The subscriber prematurely exited the enumeration loop.
    /// </summary>
    EnumerationAborted = 1,

    /// <summary>
    ///     The sequence was incomplete.
    /// </summary>
    IncompleteSequence = 2,

    /// <summary>
    ///     The consumer aborted the sequence because it is disconnecting (or rebalancing).
    /// </summary>
    ConsumerAborted = 3,

    /// <summary>
    ///     The sequence is being disposed before it could complete.
    /// </summary>
    Disposing = 4,

    /// <summary>
    ///     The sequence was aborted because of an exception thrown either by the consumer pipeline or the subscriber.
    /// </summary>
    Error = 5
}
