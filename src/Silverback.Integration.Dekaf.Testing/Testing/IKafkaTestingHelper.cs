// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Testing;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Testing;

/// <summary>
///     Exposes some helper methods to simplify testing against the Dekaf in-memory cluster.
/// </summary>
/// <remarks>
///     It's similar to the <c>IKafkaTestingHelper</c> of Silverback.Integration.Kafka.Testing, so that most tests can
///     be shared. The topics are read through the <see cref="Cluster" /> instead of the <c>GetTopic</c> method.
/// </remarks>
public interface IKafkaTestingHelper : ITestingHelper
{
    /// <summary>
    ///     Gets the in-memory cluster.
    /// </summary>
    InMemoryKafkaCluster Cluster { get; }

    /// <summary>
    ///     Gets the consumer groups joined by at least one consumer, including the consumers already closed.
    /// </summary>
    IReadOnlyCollection<IMockedConsumerGroup> ConsumerGroups { get; }

    /// <summary>
    ///     Gets the consumer group with the specified id.
    /// </summary>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedConsumerGroup" />.
    /// </returns>
    IMockedConsumerGroup GetConsumerGroup(string groupId);

    /// <summary>
    ///     Creates a new producer connected to the in-memory cluster.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An action that configures the producer.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" />.
    /// </returns>
    IProducer GetProducer(Action<KafkaProducerConfigurationBuilder> configurationBuilderAction);

    /// <summary>
    ///     Waits until the offsets of all the messages in the specified endpoints have been committed.
    /// </summary>
    /// <param name="endpointNames">
    ///     The names of the endpoints to be considered. All endpoints if empty.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(params string[] endpointNames);

    /// <summary>
    ///     Waits until the offsets of all the messages in the specified endpoints have been committed.
    /// </summary>
    /// <param name="throwTimeoutException">
    ///     A value indicating whether a <see cref="TimeoutException" /> is thrown when the timeout elapses.
    /// </param>
    /// <param name="timeout">
    ///     The timeout. The default is 30 seconds.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, TimeSpan? timeout = null);

    /// <summary>
    ///     Waits until the offsets of all the messages in the specified endpoints have been committed.
    /// </summary>
    /// <param name="throwTimeoutException">
    ///     A value indicating whether a <see cref="TimeoutException" /> is thrown when the timeout elapses.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the wait.
    /// </param>
    /// <param name="endpointNames">
    ///     The names of the endpoints to be considered. All endpoints if empty.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask WaitUntilAllMessagesAreCommittedAsync(bool throwTimeoutException, CancellationToken cancellationToken, params string[] endpointNames);
}
