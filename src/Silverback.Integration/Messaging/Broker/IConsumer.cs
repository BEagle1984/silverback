// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Consumes from one or more endpoints and pushes the received messages to the internal bus.
/// </summary>
public interface IConsumer : IBrokerConnectedObject
{
    /// <summary>
    ///     Gets the configuration.
    /// </summary>
    ConsumerConfiguration Configuration { get; }

    /// <summary>
    ///     Gets a value indicating whether this consumer is connected and consuming (started).
    /// </summary>
    // TODO: Rename or make private as it is confusing with ConsumerStatus.Consuming
    bool IsConsuming { get; }

    /// <summary>
    ///     Gets the <see cref="IConsumerStatusInfo" /> containing the status details and basic statistics of this
    ///     consumer.
    /// </summary>
    IConsumerStatusInfo StatusInfo { get; }

    /// <summary>
    ///     Gets the <see cref="ISequenceStoreCollection" /> used by this consumer. Some brokers will require
    ///     multiple stores (e.g. the <c>KafkaConsumer</c> will create a store per each assigned partition).
    /// </summary>
    ISequenceStoreCollection SequenceStores { get; }

    /// <summary>
    ///     Stops the consumer and starts an asynchronous <see cref="Task" /> to disconnect and reconnect it.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. This <see cref="Task" /> will complete as
    ///     soon as the stopping signal has been sent, while the process will be completed in another asynchronous
    ///     <see cref="Task" />.
    /// </returns>
    Task TriggerReconnectAsync();

    /// <summary>
    ///     Starts consuming. Used after <see cref="StopAsync" /> has been called to resume consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task StartAsync();

    /// <summary>
    ///     Stops the consumer without disconnecting. Can be used to pause and resume consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. This <see cref="Task" /> will complete as
    ///     soon as the stopping signal has been sent.
    /// </returns>
    Task StopAsync();

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
    Task CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier);

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
    Task CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

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
    Task RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier);

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
    Task RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

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
