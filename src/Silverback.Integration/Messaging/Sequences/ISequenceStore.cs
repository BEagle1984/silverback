// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Sequences.Unbounded;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     The temporary store for the sequences being consumed.
/// </summary>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Avoid confusion with ISequenceStoreCollection")]
public interface ISequenceStore : IReadOnlyCollection<ISequence>, IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     Gets a value indicating whether the store has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    ///     Gets the sequence with the specified id.
    /// </summary>
    /// <typeparam name="TSequence">
    ///     The type of the sequence to be retrieved.
    /// </typeparam>
    /// <param name="sequenceId">
    ///     The sequence identifier.
    /// </param>
    /// <param name="matchPrefix">
    ///     Enables sequence id prefix matching (String.StartsWith).
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="ISequence" /> instance.
    /// </returns>
    ValueTask<TSequence?> GetAsync<TSequence>(string sequenceId, bool matchPrefix = false)
        where TSequence : class, ISequence;

    /// <summary>
    ///     Adds the specified sequence to the store.
    /// </summary>
    /// <typeparam name="TSequence">
    ///     The type of the sequence to be added.
    /// </typeparam>
    /// <param name="sequence">
    ///     The sequence to be added.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="ISequence" /> instance.
    /// </returns>
    ValueTask<TSequence> AddAsync<TSequence>(TSequence sequence)
        where TSequence : class, ISequence;

    /// <summary>
    ///     Removes the sequence with the specified id.
    /// </summary>
    /// <param name="sequenceId">
    ///     The sequence identifier.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask RemoveAsync(string sequenceId);

    /// <summary>
    ///     Returns the pending sequences currently in the store.
    /// </summary>
    /// <param name="includeUnbounded">
    ///     A value indicating whether the <see cref="UnboundedSequence" /> instances have to be returned as well.
    /// </param>
    /// <param name="includeChildren">
    ///     A value indicating whether the child sequences have to be returned as well (e.g., a chunk sequence inside a batch sequence).
    /// </param>
    /// <returns>
    ///     The collection of sequences.
    /// </returns>
    IReadOnlyCollection<ISequence> GetPendingSequences(bool includeUnbounded = false, bool includeChildren = false);
}
