// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Testing
{
    /// <summary>
    ///     Exposes some helper methods to simplify testing.
    /// </summary>
    public interface IKafkaTestingHelper
    {
        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <param name="timeout">
        ///     The timeout after which the method will return even if the messages haven't been
        ///     processed. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics to be monitored.
        /// </param>
        /// <param name="timeout">
        ///     The timeout after which the method will return even if the messages haven't been
        ///     processed. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(string[] topicNames, TimeSpan? timeout = null);
    }
}
