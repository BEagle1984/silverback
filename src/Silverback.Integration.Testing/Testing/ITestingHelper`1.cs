// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

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
        ///     Gets the <see cref="IIntegrationSpy" />.
        /// </summary>
        /// <remarks>
        ///     The <see cref="IIntegrationSpy" /> must be enabled calling <c>AddIntegrationSpy</c> or
        ///     <c>AddIntegrationSpyAndSubscriber</c>.
        /// </remarks>
        IIntegrationSpy Spy { get; }

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all consumers are connected and ready.
        /// </summary>
        /// <param name="timeout">
        ///     The time to wait for the consumers to connect. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all consumers are successfully connected and ready.
        /// </returns>
        Task WaitUntilConnectedAsync(TimeSpan? timeout = null);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all consumers are connected and ready.
        /// </summary>
        /// <param name="throwTimeoutException">
        ///     A value specifying whether a <see cref="TimeoutException"/> has to be thrown when the connection
        ///     isn't established before the timeout expires.
        /// </param>
        /// <param name="timeout">
        ///     The time to wait for the consumers to connect. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all consumers are successfully connected and ready.
        /// </returns>
        Task WaitUntilConnectedAsync(bool throwTimeoutException, TimeSpan? timeout = null);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all consumers are connected and ready.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all consumers are successfully connected and ready.
        /// </returns>
        Task WaitUntilConnectedAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all consumers are connected and ready.
        /// </summary>
        /// <param name="throwTimeoutException">
        ///     A value specifying whether a <see cref="TimeoutException"/> has to be thrown when the connection
        ///     isn't established before the <see cref="CancellationToken"/> is canceled.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all consumers are successfully connected and ready.
        /// </returns>
        Task WaitUntilConnectedAsync(bool throwTimeoutException, CancellationToken cancellationToken);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked brokers only.
        /// </remarks>
        /// <param name="timeout">
        ///     The time to wait for the messages to be consumed and processed. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(TimeSpan? timeout = null);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked brokers only.
        /// </remarks>
        /// <param name="throwTimeoutException">
        ///     A value specifying whether a <see cref="TimeoutException"/> has to be thrown when the messages
        ///     aren't consumed before the timeout expires.
        /// </param>
        /// <param name="timeout">
        ///     The time to wait for the messages to be consumed and processed. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(bool throwTimeoutException, TimeSpan? timeout = null);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked brokers only.
        /// </remarks>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked brokers only.
        /// </remarks>
        /// <param name="throwTimeoutException">
        ///     A value specifying whether a <see cref="TimeoutException"/> has to be thrown when the messages
        ///     aren't consumed before the <see cref="CancellationToken"/> is canceled.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(bool throwTimeoutException, CancellationToken cancellationToken);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages stored in the outbox have been produced.
        /// </summary>
        /// <param name="timeout">
        ///     The time to wait for the messages to be consumed and processed. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when the outbox is empty.
        /// </returns>
        Task WaitUntilOutboxIsEmptyAsync(TimeSpan? timeout = null);

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

        /// <summary>
        ///     Checks whether the outbox (table) is empty.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains
        ///     <c>true</c> if the outbox is empty, otherwise <c>false</c>.
        /// </returns>
        Task<bool> IsOutboxEmptyAsync();
    }
}
