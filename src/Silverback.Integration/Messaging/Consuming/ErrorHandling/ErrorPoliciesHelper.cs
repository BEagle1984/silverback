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
    public static async Task<bool> ApplyErrorPoliciesAsync(
        ConsumerPipelineContext context,
        Exception exception)
    {
        int failedAttempts = context.Consumer.IncrementFailedAttempts(context.Envelope);

        context.Envelope.Headers.AddOrReplace(DefaultMessageHeaders.FailedAttempts, failedAttempts);

        ErrorPolicyCallbacksInvoker callbacksInvoker = context.ServiceProvider.GetRequiredService<ErrorPolicyCallbacksInvoker>();
        IErrorPolicyImplementation errorPolicyImplementation =
            context.Envelope.Endpoint.Configuration.ErrorPolicy.Build(context.ServiceProvider);

        bool handled;

        try
        {
            handled = errorPolicyImplementation.CanHandle(context, exception) &&
                      await errorPolicyImplementation.HandleErrorAsync(context, exception).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The consumer will be stopped because the error handling failed
            await TryInvokeStopCallbacksAsync(callbacksInvoker, context.Envelope, exception).ConfigureAwait(false);
            await TryStopConsumerAsync(context).ConfigureAwait(false);
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
            // The error handling failed, so the rollback won't stop the consumer
            await TryStopConsumerAsync(context).ConfigureAwait(false);
            throw;
        }

        return false;
    }

    // Stop the consumer when the error handling fails, before rethrowing the exception: no reader rethrows it when no reader awaits the
    // failed sequence (e.g., a batch completed by its max wait time), and the consumer must be stopped before the next sequence can start
    // and commit the failed one with it (Kafka)
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged by the consumer")]
    private static async Task TryStopConsumerAsync(ConsumerPipelineContext context)
    {
        try
        {
            if (context.TransactionManager is ConsumerTransactionManager transactionManager)
                await transactionManager.TryStopConsumerAsync().ConfigureAwait(false);
            else
                await context.Consumer.StopAsync(context.Envelope.BrokerMessageIdentifier, false).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The exception thrown by the error handling is rethrown instead
        }
    }

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
