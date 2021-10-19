// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     The basic interface to interact with the message broker.
/// </summary>
public interface IBroker
{
    /// <summary>
    ///     Gets the type of the <see cref="ProducerConfiguration" /> that is used by this broker implementation.
    /// </summary>
    Type ProducerConfigurationType { get; }

    /// <summary>
    ///     Gets the type of the <see cref="ConsumerConfiguration" /> that is used by this broker implementation.
    /// </summary>
    Type ConsumerConfigurationType { get; }

    /// <summary>
    ///     Gets the collection of <see cref="IProducer" /> that have been created so far.
    /// </summary>
    IReadOnlyList<IProducer> Producers { get; }

    /// <summary>
    ///     Gets the collection of <see cref="IConsumer" /> that have been created so far.
    /// </summary>
    IReadOnlyList<IConsumer> Consumers { get; }

    /// <summary>
    ///     Gets a value indicating whether this broker is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Returns an <see cref="IProducer" /> to be used to produce to the specified endpoint.
    /// </summary>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" /> for the specified endpoint.
    /// </returns>
    IProducer GetProducer(ProducerConfiguration configuration);

    /// <summary>
    ///     Returns an existing <see cref="IProducer" /> to be used to produce to the specified endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The target endpoint name (or friendly name).
    /// </param>
    /// <returns>
    ///     The <see cref="IProducer" /> for the specified endpoint.
    /// </returns>
    IProducer GetProducer(string endpointName);

    /// <summary>
    ///     Adds an <see cref="IConsumer" /> that will consume from the specified endpoint as soon as the broker
    ///     is connected. The received messages will be forwarded to the specified callback delegate.
    /// </summary>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IConsumer" /> for the specified endpoint.
    /// </returns>
    IConsumer AddConsumer(ConsumerConfiguration configuration);

    /// <summary>
    ///     Connect to the message broker to start consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task ConnectAsync();

    /// <summary>
    ///     Disconnect from the message broker to stop consuming.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task DisconnectAsync();
}
