// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Consuming.Transaction;

/// <summary>
///     Handles the consumer transaction.
/// </summary>
public interface IConsumerTransactionManager
{
    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is fired before the consumer is committed.
    /// </summary>
    /// <remarks>
    ///     It isn't fired when the consumer is committed by the rollback, without processing the message (see <see cref="Skipping" />).
    /// </remarks>
    AsyncEvent<ConsumerPipelineContext> Committing { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is after the consumer is committed.
    /// </summary>
    AsyncEvent<ConsumerPipelineContext> Committed { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is before the consumer is aborted.
    /// </summary>
    AsyncEvent<ConsumerPipelineContext> Aborting { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is after the consumer is aborted.
    /// </summary>
    AsyncEvent<ConsumerPipelineContext> Aborted { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is fired when the rollback commits the consumer without processing the message,
    ///     for example when the message is skipped or moved by the error policies or when an incomplete sequence is aborted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It's fired right before the consumer is committed (after <see cref="Aborting" /> and before <see cref="Aborted" />) and
    ///         only if the commit actually happens. The commit is deferred, and the event isn't fired, when other sequences are pending
    ///         or still being processed, or when the consumer is stopping. <see cref="Committing" /> and <see cref="Committed" /> aren't
    ///         fired in this case.
    ///     </para>
    ///     <para>
    ///         The transaction failed: the handlers must not rely on the storage transaction enlisted by the subscribers, which is
    ///         rolled back. An exception thrown by a handler fails the rollback and the consumer isn't committed.
    ///     </para>
    /// </remarks>
    AsyncEvent<ConsumerPipelineContext> Skipping { get; }

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     The transaction was already aborted (or is being aborted).
    /// </exception>
    Task CommitAsync();

    /// <summary>
    ///     Aborts the transaction and causes the uncommitted changes to be rolled back.
    /// </summary>
    /// <param name="exception">
    ///     The exception that caused the rollback.
    /// </param>
    /// <param name="commitConsumer">
    ///     A value indicating whether the consumer have to be committed anyway. This depends on the error policy
    ///     being applied.
    /// </param>
    /// <param name="throwIfAlreadyCommitted">
    ///     A value indicating whether an exception must be thrown if the transaction was already committed.
    /// </param>
    /// <param name="stopConsuming">
    ///     A value indicating whether the consumer must be stopped.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
    ///     value indicating whether the rollback was performed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     The transaction was already committed (or is being committed) and <paramref name="throwIfAlreadyCommitted" /> is <c>true</c>.
    /// </exception>
    Task<bool> RollbackAsync(
        Exception? exception,
        bool commitConsumer = false,
        bool throwIfAlreadyCommitted = true,
        bool stopConsuming = true);
}
