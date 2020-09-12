// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The error policies factory.
    /// </summary>
    public interface IErrorPolicyBuilder
    {
        /// <summary>
        ///     Creates a chain of multiple policies to be applied one after the other to handle the processing
        ///     error.
        /// </summary>
        /// <param name="policies">
        ///     The policies to be sequentially applied.
        /// </param>
        /// <returns>
        ///     The created <see cref="ErrorPolicyChain" /> initialized with the specified policies.
        /// </returns>
        ErrorPolicyChain Chain(params ErrorPolicyBase[] policies);

        /// <summary>
        ///     Creates a retry policy to simply try again the message processing in case of processing errors.
        /// </summary>
        /// <param name="initialDelay">
        ///     The optional delay between each retry.
        /// </param>
        /// <param name="delayIncrement">
        ///     The optional increment to be added to the initial delay at each retry.
        /// </param>
        /// <returns>
        ///     The created <see cref="RetryErrorPolicy" />.
        /// </returns>
        RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null);

        /// <summary>
        ///     Creates a skip policy to discard the message whose processing failed.
        /// </summary>
        /// <returns>
        ///     The created <see cref="SkipMessageErrorPolicy" />.
        /// </returns>
        SkipMessageErrorPolicy Skip();

        /// <summary>
        ///     Creates a move policy to forward the message to another endpoint in case of processing errors.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to move the message to.
        /// </param>
        /// <returns>
        ///     The created <see cref="MoveMessageErrorPolicy" />.
        /// </returns>
        MoveMessageErrorPolicy Move(IProducerEndpoint endpoint);
    }
}
