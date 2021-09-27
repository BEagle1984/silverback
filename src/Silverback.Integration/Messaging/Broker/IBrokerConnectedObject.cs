// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The common interface implemented by both <see cref="IProducer" /> and <see cref="IConsumer" />.
    /// </summary>
    public interface IBrokerConnectedObject
    {
        /// <summary>
        ///     Gets the <see cref="InstanceIdentifier" /> uniquely identifying the instance.
        /// </summary>
        InstanceIdentifier Id { get; }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this <see cref="IProducer" /> or <see cref="IConsumer" />.
        /// </summary>
        IBroker Broker { get; }

        /// <summary>
        ///     Gets a value indicating whether this producer or consumer is connected to the message broker and ready to produce
        ///     or consume messages.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        ///     Gets a value indicating whether this producer or consumer is trying to connect to the message broker.
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        ///     Initializes the connection to the message broker (if needed).
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ConnectAsync();

        /// <summary>
        ///     Disconnects from the message broker (if needed).
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task DisconnectAsync();
    }
}
