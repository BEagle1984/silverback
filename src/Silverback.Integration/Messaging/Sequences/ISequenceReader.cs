// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     Can recognize a message that belongs to a sequence and match it with the proper
///     <see cref="ISequence" /> instance.
/// </summary>
/// <remarks>
///     The messages skipped without being delivered, because filtered out (see <see cref="ConsumerEndpointConfiguration.Filter" />) or
///     ignored because no subscriber handles them (see <see cref="ConsumerEndpointConfiguration.ThrowIfUnhandled" />), aren't added to the
///     sequence they belong to and don't count toward its length. A custom sequence completed by its last message, or by a total length
///     equal to the fixed number of messages it is made of, therefore isn't completed when that message (or any message, with a total
///     length) is skipped, and is eventually aborted as incomplete. The built-in batches, chunked messages and streams aren't affected.
/// </remarks>
public interface ISequenceReader
{
    /// <summary>
    ///     Gets a value indicating whether this reader handles the raw messages, before they are being
    ///     deserialized, decrypted, etc.
    /// </summary>
    bool HandlesRawMessages { get; }

    /// <summary>
    ///     Checks whether this reader can and must handle the message being processed in the specified context.
    /// </summary>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
    ///     value indicating whether this reader can and must handle the message.
    /// </returns>
    ValueTask<bool> CanHandleAsync(ConsumerPipelineContext context);

    /// <summary>
    ///     Returns the <see cref="ISequence" /> related to the message being processed.
    /// </summary>
    /// <param name="context">
    ///     The current <see cref="ConsumerPipelineContext" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="ISequence" />.
    /// </returns>
    ValueTask<ISequence> GetSequenceAsync(ConsumerPipelineContext context);
}
