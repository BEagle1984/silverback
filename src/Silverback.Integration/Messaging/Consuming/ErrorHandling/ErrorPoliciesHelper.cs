// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Consuming.ErrorHandling;

internal static class ErrorPoliciesHelper
{
    // The transaction of the context is rolled back when the error handling fails, unless isRollbackOnFailure is false because the
    // context isn't the one of the failed message (e.g., the unbounded sequence aborted while no message was being added, whose
    // context is the one of the message that started it, committed by its own pipeline): the consumer is just stopped then.
    // When isStopIfRollbackPrevented is true and the transaction can't be rolled back anymore, because the commit of the message failed
    // after the consumer was committed (e.g., a Committed handler threw), the error policies aren't applied and the error is handled as
    // when no policy handles it, stopping the consumer: retrying, skipping or moving the message requires rolling back its transaction,
    // and would only invoke the callbacks of the policy, fail to roll back the already processed and committed message and reconnect.
    public static async Task<bool> ApplyErrorPoliciesAsync(
        ConsumerPipelineContext context,
        Exception exception,
        bool isRollbackOnFailure = true,
        bool isStopIfRollbackPrevented = false)
    {
        ErrorPolicyCallbacksInvoker? callbacksInvoker = null;
        bool handled;

        // The bookkeeping is part of the error handling as well: its failure rolls back and stops the consumer
        try
        {
            callbacksInvoker = context.ServiceProvider.GetRequiredService<ErrorPolicyCallbacksInvoker>();

            int failedAttempts = context.Consumer.IncrementFailedAttempts(context.Envelope);
            context.Envelope.Headers.AddOrReplace(DefaultMessageHeaders.FailedAttempts, failedAttempts);

            IErrorPolicyImplementation errorPolicyImplementation =
                context.Envelope.Endpoint.Configuration.ErrorPolicy.Build(context.ServiceProvider);

            handled = !(isStopIfRollbackPrevented && IsRollbackPrevented(context)) &&
                      errorPolicyImplementation.CanHandle(context, exception) &&
                      await errorPolicyImplementation.HandleErrorAsync(context, exception).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The consumer will be stopped because the error handling failed
            if (callbacksInvoker != null)
                await TryInvokeStopCallbacksAsync(callbacksInvoker, context.Envelope, exception).ConfigureAwait(false);

            await TryRollbackAndStopConsumerAsync(context, exception, isRollbackOnFailure).ConfigureAwait(false);
            throw;
        }

        if (handled)
            return true;

        // The consumer is stopped whenever the error isn't handled, either explicitly by the StopConsumerErrorPolicy
        // or because no policy could handle it
        try
        {
            await InvokeStopCallbacksAsync(callbacksInvoker, context.Envelope, exception).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The error handling failed, so the caller won't roll back and stop the consumer
            await TryRollbackAndStopConsumerAsync(context, exception, isRollbackOnFailure).ConfigureAwait(false);
            throw;
        }

        return false;
    }

    // The error policies don't handle the error: roll back the transaction, which stops the consumer. The transaction can't be rolled back
    // anymore when the commit failed after the consumer was committed (e.g., a Committed handler threw): just stop the consumer then, as
    // when the commit of the sequence whose subscribers stopped enumerating it fails (see SequenceBase.HandleCommitFailureAsync), instead
    // of failing the rollback. The failed rollback would leave the consumer running when no reader awaits the sequence (e.g., a batch
    // completed by its max wait time) and rethrow the rollback exception instead of the commit exception otherwise, to the reader of the
    // sequence or to the pipeline of the message that doesn't belong to a sequence, which logs it as fatal and disconnects the client.
    public static async Task RollbackOrStopConsumerAsync(ConsumerPipelineContext context, Exception exception)
    {
        if (context.TransactionManager is not ConsumerTransactionManager transactionManager)
        {
            await context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);
            return;
        }

        if (!await transactionManager.RollbackAsync(exception, throwIfAlreadyCommitted: false).ConfigureAwait(false))
            await transactionManager.RollbackAndStopConsumerAfterFailureAsync(exception).ConfigureAwait(false);
    }

    // Stop the consumer when the error handling fails, before rethrowing the exception: no reader rethrows it when no reader awaits the
    // failed sequence (e.g., a batch completed by its max wait time), and the consumer must be stopped before the next sequence can start
    // and commit the failed one with it (Kafka). The transaction is rolled back first, without committing the consumer, as when the error
    // policies don't handle the error, raising the Aborting and Aborted events: it would otherwise be left neither committed nor rolled
    // back. It's just stopped if the transaction was already completed or aborted (e.g., by the error policy, before failing). The rollback
    // never throws and logs its own failure, while the failure of the stop alone is logged by the consumer.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The stop failure is logged by the consumer, the rollback failure by the transaction manager")]
    private static async Task TryRollbackAndStopConsumerAsync(ConsumerPipelineContext context, Exception exception, bool isRollbackOnFailure)
    {
        try
        {
            if (context.TransactionManager is not ConsumerTransactionManager transactionManager)
                await context.Consumer.StopAsync(context.Envelope.BrokerMessageIdentifier, false).ConfigureAwait(false);
            else if (isRollbackOnFailure)
                await transactionManager.RollbackAndStopConsumerAfterFailureAsync(exception).ConfigureAwait(false);
            else
                await transactionManager.TryStopConsumerAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The exception thrown by the error handling is rethrown instead
        }
    }

    private static bool IsRollbackPrevented(ConsumerPipelineContext context) =>
        context.TransactionManager is ConsumerTransactionManager { IsRollbackPrevented: true };

    private static ValueTask InvokeStopCallbacksAsync(
        ErrorPolicyCallbacksInvoker callbacksInvoker,
        IRawInboundEnvelope envelope,
        Exception exception) =>
        callbacksInvoker.InvokeAsync<IStopConsumerErrorPolicyCallback>(callback => callback.OnStoppingConsumerAsync(envelope, exception));

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged by the invoker")]
    private static async Task TryInvokeStopCallbacksAsync(
        ErrorPolicyCallbacksInvoker callbacksInvoker,
        IRawInboundEnvelope envelope,
        Exception exception)
    {
        try
        {
            await InvokeStopCallbacksAsync(callbacksInvoker, envelope, exception).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Already logged by the invoker. The exception thrown by the error handling is rethrown instead.
        }
    }
}
