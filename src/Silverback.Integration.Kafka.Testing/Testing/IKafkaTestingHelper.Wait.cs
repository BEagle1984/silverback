// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Testing;

/// <content>
///     Declares the Kafka commit wait methods.
/// </content>
public partial interface IKafkaTestingHelper
{
    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(params string[] endpointNames);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="timeout">
    ///     The time to wait for the message offsets to be committed. The default is 30 seconds.
    /// </param>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(TimeSpan? timeout, params string[] endpointNames);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="throwTimeoutException">
    ///     A value specifying whether a <see cref="TimeoutException" /> has to be thrown when the offsets aren't committed before the
    ///     timeout elapses.
    /// </param>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, params string[] endpointNames);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="throwTimeoutException">
    ///     A value specifying whether a <see cref="TimeoutException" /> has to be thrown when the offsets aren't committed before the
    ///     timeout elapses.
    /// </param>
    /// <param name="timeout">
    ///     The time to wait for the message offsets to be committed. The default is 30 seconds.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, TimeSpan? timeout = null);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="throwTimeoutException">
    ///     A value specifying whether a <see cref="TimeoutException" /> has to be thrown when the offsets aren't committed before the
    ///     timeout elapses.
    /// </param>
    /// <param name="timeout">
    ///     The time to wait for the message offsets to be committed. The default is 30 seconds.
    /// </param>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, TimeSpan? timeout, params string[] endpointNames);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(CancellationToken cancellationToken, params string[] endpointNames);

    /// <summary>
    ///     Returns a <see cref="ValueTask" /> that completes when the offsets of all messages routed to the consumers
    ///     have been committed to the mocked Kafka broker.
    /// </summary>
    /// <remarks>
    ///     This method works with the mocked Kafka broker only and does not force offset commits.
    /// </remarks>
    /// <param name="throwTimeoutException">
    ///     A value specifying whether a <see cref="TimeoutException" /> has to be thrown when the offsets
    ///     aren't committed before the <see cref="CancellationToken" /> is canceled.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <param name="endpointNames">
    ///     The names of the endpoints to wait for. If not specified, all endpoints are considered.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> that completes when all message offsets have been committed.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(
        bool throwTimeoutException,
        CancellationToken cancellationToken,
        params string[] endpointNames);
}
