// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     Declares the <see cref="OnStoppingConsumerAsync" /> callback.
/// </summary>
public interface IStopConsumerErrorPolicyCallback : IErrorPolicyCallback
{
    /// <summary>
    ///     Called when the consumer is about to be stopped because of a processing error, either because the
    ///     <see cref="StopConsumerErrorPolicy" /> is applied or because no other policy could handle the error (e.g. after
    ///     the maximum number of retries has been reached).
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message that couldn't be processed. It's an <see cref="IInboundEnvelope" /> only if the
    ///     message was successfully deserialized, since the error could have occurred before or during the deserialization.
    /// </param>
    /// <param name="exception">
    ///     The exception that caused the consumer to be stopped.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task OnStoppingConsumerAsync(IRawInboundEnvelope envelope, Exception exception);
}
