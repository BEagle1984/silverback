// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     Declares the <see cref="OnRetryingMessageAsync" /> callback.
/// </summary>
public interface IRetryErrorPolicyCallback : IErrorPolicyCallback
{
    /// <summary>
    ///     Called when the <see cref="RetryErrorPolicy" /> is about to be applied, before the transaction is rolled back
    ///     and the delay is applied.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message that couldn't be processed. It's an <see cref="IInboundEnvelope" /> only if the
    ///     message was successfully deserialized, since the error could have occurred before or during the deserialization.
    /// </param>
    /// <param name="exception">
    ///     The exception that caused the policy to be applied.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task OnRetryingMessageAsync(IRawInboundEnvelope envelope, Exception exception);
}
