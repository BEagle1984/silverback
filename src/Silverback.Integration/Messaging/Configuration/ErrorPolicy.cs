// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Inbound.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Contains some helper methods used to build the error policies.
    /// </summary>
    public static class ErrorPolicy
    {
        /// <summary>
        ///     Builds an instance of the default <see cref="StopConsumerErrorPolicy" /> that stops the consumer when
        ///     an exception is thrown during the message processing.
        /// </summary>
        /// <returns>
        ///     The error policy instance.
        /// </returns>
        public static StopConsumerErrorPolicy Stop() => new StopConsumerErrorPolicy();

        /// <summary>
        ///     Builds an instance of the <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to
        ///     be processed.
        /// </summary>
        /// <returns>
        ///     The error policy instance.
        /// </returns>
        public static SkipMessageErrorPolicy Skip() => new SkipMessageErrorPolicy();

        /// <summary>
        ///     Builds an instance of the <see cref="RetryErrorPolicy" /> that retries to process the messages that
        ///     previously failed to be to processed.
        /// </summary>
        /// <returns>
        ///     The error policy instance.
        /// </returns>
        /// <param name="initialDelay">
        ///     The optional delay to be applied to the first retry.
        /// </param>
        /// <param name="delayIncrement">
        ///     The optional increment to the delay to be applied at each retry.
        /// </param>
        public static RetryErrorPolicy Retry(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null) =>
            new RetryErrorPolicy(initialDelay, delayIncrement);

        /// <summary>
        ///     Builds an instance of the <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to
        ///     be processed to the configured endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to move the message to.
        /// </param>
        /// ///
        /// <returns>
        ///     The error policy instance.
        /// </returns>
        public static MoveMessageErrorPolicy Move(IProducerEndpoint endpoint) =>
            new MoveMessageErrorPolicy(endpoint);

        /// <summary>
        ///     Builds a chain combining multiple error policies to be sequentially applied.
        /// </summary>
        /// <param name="policies">
        ///     The policies to be chained.
        /// </param>
        /// <returns>
        ///     The error policy instance.
        /// </returns>
        public static ErrorPolicyChain Chain(params ErrorPolicyBase[] policies) =>
            Chain(policies.AsEnumerable());

        /// <summary>
        ///     Builds a chain combining multiple error policies to be sequentially applied.
        /// </summary>
        /// <param name="policies">
        ///     The policies to be chained.
        /// </param>
        /// <returns>
        ///     The error policy instance.
        /// </returns>
        public static ErrorPolicyChain Chain(IEnumerable<ErrorPolicyBase> policies) =>
            new ErrorPolicyChain(policies);
    }
}
