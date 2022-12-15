// Copyright (c) 2023 Sergio Aquilini
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
    AsyncEvent<ConsumerPipelineContext> Committing { get; }

    /// <summary>
    ///    Gets the <see cref="AsyncEvent{TArg}" /> that is after the consumer is committed.
    /// </summary>
    AsyncEvent<ConsumerPipelineContext> Committed { get; }

    /// <summary>
    ///    Gets the <see cref="AsyncEvent{TArg}" /> that is before the consumer is aborted.
    /// </summary>
    AsyncEvent<ConsumerPipelineContext> Aborting { get; }

    /// <summary>
    ///    Gets the <see cref="AsyncEvent{TArg}" /> that is after the consumer is aborted.
    /// </summary>
    AsyncEvent<ConsumerPipelineContext> Aborted { get; }

    /// <summary>
    ///     Gets a value indicating whether the transaction has completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    ///     Commits the transaction.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
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
    Task<bool> RollbackAsync(
        Exception? exception,
        bool commitConsumer = false,
        bool throwIfAlreadyCommitted = true,
        bool stopConsuming = true);
}
