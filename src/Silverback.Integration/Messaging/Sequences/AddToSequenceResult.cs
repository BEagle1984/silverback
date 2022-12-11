// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Sequences;

/// <summary>
///     Encapsulates the result of the <see cref="ISequence.AddAsync" /> method.
/// </summary>
/// <param name="IsSuccess">
///     A value indicating whether the operation was successful or not.
/// </param>
/// <param name="PushedStreamsCount">
///     The number of streams that have been actually pushed.
/// </param>
public readonly record struct AddToSequenceResult(bool IsSuccess, int PushedStreamsCount)
{
    /// <summary>
    ///     Gets a static instance representing a failed call to <see cref="ISequence.AddAsync" />.
    /// </summary>
    public static AddToSequenceResult Failed { get; } = new(false, -1);

    /// <summary>
    ///     Returns a new instance representing a successful call to <see cref="ISequence.AddAsync" />.
    /// </summary>
    /// <param name="pushedStreams">
    ///     The number of streams that have been actually pushed.
    /// </param>
    /// <returns>
    ///     The <see cref="AddToSequenceResult" />.
    /// </returns>
    public static AddToSequenceResult Success(int pushedStreams) => new(true, pushedStreams);
}
