// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Testing
{
    /// <summary>
    ///     Exposes some helper methods and shortcuts to simplify testing.
    /// </summary>
    /// <typeparam name="TBroker">
    ///     The <see cref="IBroker" /> implementation.
    /// </typeparam>
    public interface ITestingHelper<out TBroker>
        where TBroker : IBroker
    {
        /// <summary>
        ///     Gets the current <see cref="IBroker" /> instance.
        /// </summary>
        TBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IOutboxReader"/>.
        /// </summary>
        IOutboxReader OutboxReader { get; }

        /// <summary>
        ///     Gets the <see cref="IIntegrationSpy" />.
        /// </summary>
        /// <remarks>
        ///     The <see cref="IIntegrationSpy" /> must be enabled calling <c>AddIntegrationSpy</c> or
        ///     <c>AddIntegrationSpyAndSubscriber</c>.
        /// </remarks>
        IIntegrationSpy Spy { get; }

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
        ///     Returns a <see cref="Task" /> that completes when all messages stored in the outbox have been produced.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when the outbox is empty.
        /// </returns>
        Task WaitUntilOutboxIsEmptyAsync(CancellationToken cancellationToken);
    }
}
