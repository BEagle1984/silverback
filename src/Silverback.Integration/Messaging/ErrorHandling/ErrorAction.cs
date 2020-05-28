// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    ///     Specifies the action the consumer is supposed to perform as a result of the processing exception.
    /// </summary>
    public enum ErrorAction
    {
        /// <summary>
        ///     The message must be skipped and the processing must continue with the next message.
        /// </summary>
        Skip,

        /// <summary>
        ///     The message processing has to be retried.
        /// </summary>
        Retry,

        /// <summary>
        ///     A fatal error occurred and the consumer has to be stopped.
        /// </summary>
        StopConsuming
    }
}
