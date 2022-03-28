// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     An error policy is used to handle errors that may occur while processing the inbound messages.
/// </summary>
public interface IErrorPolicyImplementation
{
    /// <summary>
    ///     Returns a boolean value indicating whether the policy can handle the specified envelopes and the
    ///     specified exception.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ConsumerPipelineContext" /> related to the message that failed to be processed.
    /// </param>
    /// <param name="exception">
    ///     The exception that was thrown during the processing.
    /// </param>
    /// <returns>
    ///     A value indicating whether the specified envelopes and exception can be handled.
    /// </returns>
    bool CanHandle(ConsumerPipelineContext context, Exception exception);

    /// <summary>
    ///     Performs the necessary actions to handle the error (including invoking the
    ///     <see cref="IConsumerTransactionManager" />).
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ConsumerPipelineContext" /> related to the message that failed to be processed.
    /// </param>
    /// <param name="exception">
    ///     The exception that was thrown during the processing.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
    ///     boolean flag indicating whether the error was handled. If <c>false</c> is returned the exception will
    ///     be rethrown and the consumer will stop.
    /// </returns>
    Task<bool> HandleErrorAsync(ConsumerPipelineContext context, Exception exception);
}
