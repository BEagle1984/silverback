// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     Declares the <see cref="OnSkippingMessageAsync" /> callback.
/// </summary>
public interface ISkipMessageErrorPolicyCallback : IErrorPolicyCallback
{
    /// <summary>
    ///     Called when the <see cref="SkipMessageErrorPolicy" /> is about to be applied, before the message is skipped
    ///     and its offset committed.
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
    Task OnSkippingMessageAsync(IRawInboundEnvelope envelope, Exception exception);
}
