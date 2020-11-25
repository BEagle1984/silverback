// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Consumes an endpoint and invokes a callback delegate when a message is received.
    /// </summary>
    public interface IConsumer
    {
        /// <summary>
        ///     Gets the <see cref="Guid"/> uniquely identifying the consumer instance.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> that owns this consumer.
        /// </summary>
        IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> representing the endpoint that is being consumed.
        /// </summary>
        IConsumerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets a value indicating whether this consumer is connected to the message broker and ready to consume
        ///     messages.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Gets a value indicating whether this consumer is connected and consuming.
        /// </summary>
        bool IsConsuming { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerStatusInfo" /> containing the status details and basic statistics of this
        ///     consumer.
        /// </summary>
        IConsumerStatusInfo StatusInfo { get; }

        /// <summary>
        ///     <param>
        ///         Confirms that the message at the specified offset has been successfully processed.
        ///     </param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed
        ///         again (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offset">
        ///     The offset to be committed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CommitAsync(IOffset offset);

        /// <summary>
        ///     <param>
        ///         Confirms that the messages at the specified offsets have been successfully processed.
        ///     </param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed
        ///         again (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <param name="offsets">
        ///     The offsets to be committed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CommitAsync(IReadOnlyCollection<IOffset> offsets);

        /// <summary>
        ///     <param>
        ///         Notifies that an error occured while processing the message at the specified offset.
        ///     </param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will
        ///         be re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offset">
        ///     The offset to be rolled back.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RollbackAsync(IOffset offset);

        /// <summary>
        ///     <param>
        ///         Notifies that an error occured while processing the messages at the specified offsets.
        ///     </param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will
        ///         be re-processed.
        ///     </param>
        /// </summary>
        /// <param name="offsets">
        ///     The offsets to be rolled back.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RollbackAsync(IReadOnlyCollection<IOffset> offsets);

        /// <summary>
        ///     Connects and starts consuming.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ConnectAsync();

        /// <summary>
        ///     Disconnects and stops consuming.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task DisconnectAsync();

        /// <summary>
        ///     Starts consuming. Used after <see cref="Stop"/> has been called to resume consuming.
        /// </summary>
        void Start();

        /// <summary>
        ///     Stops the consumer without disconnecting. Can be used to pause and resume consuming.
        /// </summary>
        void Stop();

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

        /// <summary>
        ///     Removes the stored failed attempts counter for the specified envelope.
        /// </summary>
        /// <param name="envelope">
        ///     The offset.
        /// </param>
        void ClearFailedAttempts(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Gets the <see cref="ISequenceStore" /> instances used by this consumer. Some brokers will require
        ///     multiple stores (e.g. the <c>KafkaConsumer</c> will create a store per each assigned partition).
        /// </summary>
        /// <returns>
        ///    The list of <see cref="ISequenceStore"/>.
        /// </returns>
        IReadOnlyList<ISequenceStore> GetCurrentSequenceStores();
    }
}
