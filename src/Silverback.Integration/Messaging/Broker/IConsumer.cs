// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Consumes from one or more endpoints and pushes the received messages to the internal bus.
/// </summary>
public interface IConsumer
{
    /// <summary>
    ///     Gets the consumer name.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     Gets the related <see cref="IBrokerClient" />.
    /// </summary>
    IBrokerClient Client { get; }

    /// <summary>
    ///     Gets the endpoints configuration.
    /// </summary>
    IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; }

    /// <summary>
    ///     Gets the <see cref="IConsumerStatusInfo" /> containing the status details and basic statistics of this consumer.
    /// </summary>
    IConsumerStatusInfo StatusInfo { get; }

    /// <summary>
    ///     Stops the consumer and starts an asynchronous <see cref="Task" /> to disconnect and reconnect it.
    /// </summary>
    /// <remarks>
    ///     This is used to recover when the consumer is stuck in state where it's not able to rollback or commit anymore.
    /// </remarks>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. This <see cref="Task" /> will complete as
    ///     soon as the stopping signal has been sent, while the process will be completed in another asynchronous
    ///     <see cref="Task" />.
    /// </returns>
    ValueTask TriggerReconnectAsync(); // TODO: Check name

    /// <summary>
    ///     Starts consuming. Used after <see cref="StopAsync" /> has been called to resume consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask StartAsync();

    /// <summary>
    ///     Stops the consumer without disconnecting. Can be used to pause and resume consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. This <see cref="Task" /> will complete as
    ///     soon as the stopping signal has been sent.
    /// </returns>
    ValueTask StopAsync();

    /// <summary>
    ///     <param>
    ///         Confirms that the specified message has been successfully processed.
    ///     </param>
    ///     <param>
    ///         The acknowledgement will be sent to the message broker and the message will never be consumed
    ///         again (by the same logical consumer / consumer group).
    ///     </param>
    /// </summary>
    /// <param name="brokerMessageIdentifier">
    ///     The identifier of the message to be committed.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier);

    /// <summary>
    ///     <param>
    ///         Confirms that the specified messages have been successfully processed.
    ///     </param>
    ///     <param>
    ///         The acknowledgement will be sent to the message broker and the message will never be consumed
    ///         again (by the same logical consumer / consumer group).
    ///     </param>
    /// </summary>
    /// <param name="brokerMessageIdentifiers">
    ///     The identifiers of to message be committed.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

    /// <summary>
    ///     <param>
    ///         Notifies that an error occured while processing the specified message.
    ///     </param>
    ///     <param>
    ///         If necessary the information will be sent to the message broker to ensure that the message will
    ///         be consumed again.
    ///     </param>
    /// </summary>
    /// <param name="brokerMessageIdentifier">
    ///     The identifier of the message to be rolled back.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier);

    /// <summary>
    ///     <param>
    ///         Notifies that an error occured while processing the specified messages.
    ///     </param>
    ///     <param>
    ///         If necessary the information will be sent to the message broker to ensure that the message will
    ///         be re-processed.
    ///     </param>
    /// </summary>
    /// <param name="brokerMessageIdentifiers">
    ///     The identifiers of to message be rolled back.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

    /// <summary>
    ///     Increments the stored failed attempts count for the specified envelope.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope.
    /// </param>
    /// <returns>
    ///     The current failed attempts count after the increment.
    /// </returns>
    int IncrementFailedAttempts(IRawInboundEnvelope envelope);
}
