// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Holds a reference to all the registered <see cref="IBroker" /> implementations and is able to
///     resolve the right instance according to the <see cref="EndpointConfiguration" /> type.
/// </summary>
public interface IBrokerCollection : IReadOnlyList<IBroker>
{
    /// <summary>
    ///     Returns an <see cref="IProducer" /> with the specified configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     <see cref="IProducer" /> with the specified configuration.
    /// </returns>
    Task<IProducer> GetProducerAsync(ProducerConfiguration configuration);

    /// <summary>
    ///     Returns an <see cref="IProducer" /> with the specified configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" /> with the specified configuration.
    /// </returns>
    IProducer GetProducer(ProducerConfiguration configuration);

    /// <summary>
    ///     Adds an <see cref="IConsumer" /> with the specified configuration that will start consuming as soon as the broker
    ///     is connected. The received messages will sent through the behaviors pipeline and forwarded to the subscribers.
    /// </summary>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IConsumer" /> with the specified configuration.
    /// </returns>
    IConsumer AddConsumer(ConsumerConfiguration configuration);

    /// <summary>
    ///     Connect to all message brokers to start consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task ConnectAsync();

    /// <summary>
    ///     Disconnect from all message brokers to stop consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task DisconnectAsync();
}
