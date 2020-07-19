﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Consumes an endpoint and invokes a callback delegate when a message is received.
    /// </summary>
    public interface IConsumer
    {
        /// <summary>
        ///     Gets the <see cref="IBroker" /> that owns this consumer.
        /// </summary>
        IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerEndpoint" /> representing the endpoint that is being consumed.
        /// </summary>
        IConsumerEndpoint Endpoint { get; }

        /// <summary>
        ///     Gets the collection of <see cref="IConsumerBehavior" /> configured for this <see cref="IConsumer" />.
        /// </summary>
        IReadOnlyCollection<IConsumerBehavior> Behaviors { get; }

        /// <summary>
        ///     Gets a value indicating whether this consumer is connected to the message broker and ready to consume
        ///     messages.
        /// </summary>
        bool IsConnected { get; }

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
        Task Commit(IOffset offset);

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
        Task Commit(IReadOnlyCollection<IOffset> offsets);

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
        Task Rollback(IOffset offset);

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
        Task Rollback(IReadOnlyCollection<IOffset> offsets);

        /// <summary>
        ///     Connects and starts consuming.
        /// </summary>
        void Connect();

        /// <summary>
        ///     Disconnects and stops consuming.
        /// </summary>
        void Disconnect();
    }
}
