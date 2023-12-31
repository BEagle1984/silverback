// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

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
/// <param name="IsAborted">
///     A value indicating whether the sequence was aborted.
/// </param>
/// <param name="AbortTask">
///    The <see cref="Task" /> representing the abort operation.
/// </param>
public readonly record struct AddToSequenceResult(bool IsSuccess, int PushedStreamsCount = -1, bool IsAborted = false, Task? AbortTask = null)
{
    /// <summary>
    ///     Gets a static instance representing a failed call to <see cref="ISequence.AddAsync" />.
    /// </summary>
    public static AddToSequenceResult Failed { get; } = new(false);

    /// <summary>
    ///     Returns a new instance representing an aborted call to <see cref="ISequence.AddAsync" />. (The sequence was probably aborted.)
    /// </summary>
    /// <param name="abortTask">
    ///     The <see cref="Task" /> representing the abort operation.
    /// </param>
    /// <returns>
    ///     The <see cref="AddToSequenceResult" />.
    /// </returns>
    public static AddToSequenceResult Aborted(Task? abortTask) => new(true, IsAborted: true, AbortTask: abortTask);

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
