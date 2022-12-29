// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Silverback.Messaging.Sequences;

internal static class SequenceExtensions
{
    /// <summary>
    ///     <para>
    ///         Awaits both the ProcessingTask and subsequently the ProcessingCompletedTask.
    ///     </para>
    ///     <para>
    ///         ProcessingTask will complete when the subscriber method completes, while
    ///         ProcessingCompletedTask will complete either when the transaction is committed
    ///         or when the sequence is aborted.
    ///     </para>
    /// </summary>
    /// <param name="sequence">
    ///     The <see cref="ISequence" />.
    /// </param>
    /// <param name="rethrowExceptions">
    ///     Specifies whether the exceptions from the asynchronous tasks have to be rethrown.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "All exceptions have to be caught and opt. rethrown")]
    [SuppressMessage("", "CA2219", Justification = "It's OK to overwrite the exception in this case")]
    public static async Task AwaitProcessingAsync(this ISequence sequence, bool rethrowExceptions)
    {
        // Await both the ProcessingTask and subsequently the ProcessingCompletedTask.
        // ProcessingTask will complete when the subscriber method completes, while
        // ProcessingCompletedTask will complete either when the transaction is committed \
        // or when the sequence is aborted.
        try
        {
            if (sequence.Context.ProcessingTask != null)
                await sequence.Context.ProcessingTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ignore this, since it was caught and handled already in the monitoring function
        }

        try
        {
            if (sequence is ISequenceImplementation sequenceImpl)
                await sequenceImpl.ProcessingCompletedTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            if (rethrowExceptions)
                throw;
        }
    }
}
