// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}"/>
    public interface IKafkaTestingHelper : ITestingHelper<KafkaBroker>
    {
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
        Task WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, TimeSpan? timeout = null);
    }
}
