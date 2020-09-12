// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     An error policy is used to handle errors that may occur while processing the inbound messages.
    /// </summary>
    public interface IErrorPolicy
    {
        /// <summary>
        ///     Returns a boolean value indicating whether the policy can handle the specified envelopes and the
        ///     specified exception.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes that failed to be processed.
        /// </param>
        /// <param name="exception">
        ///     The exception that was thrown during the processing.
        /// </param>
        /// <returns>
        ///     A value indicating whether the specified envelopes and exception can be handled.
        /// </returns>
        bool CanHandle(IReadOnlyCollection<IRawInboundEnvelope> envelopes, Exception exception);

        /// <summary>
        ///     Handles the error and returns the <see cref="ErrorAction" /> to be performed by the consumer.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes that failed to be processed.
        /// </param>
        /// <param name="exception">
        ///     The exception that was thrown during the processing.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the action
        ///     that the consumer should perform (e.g. skip the message or stop consuming).
        /// </returns>
        Task<ErrorAction> HandleError(IReadOnlyCollection<IRawInboundEnvelope> envelopes, Exception exception);
    }
}
