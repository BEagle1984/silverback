// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Wraps the underlying client libraries (e.g. Kafka consumers and producers or Mqtt clients) and handles the connection
///     lifecycle.
/// </summary>
public interface IBrokerClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Gets the client name.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is fired when the <see cref="ConnectAsync" /> method is called
    ///     and the client is initializing.
    /// </summary>
    AsyncEvent<BrokerClient> Initializing { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is fired when the <see cref="ConnectAsync" /> method has been called
    ///     and the client has been successfully initialized. The connection with the broker will eventually be established.
    /// </summary>
    AsyncEvent<BrokerClient> Initialized { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is fired when the <see cref="DisconnectAsync" /> method is called
    ///     and the client is disconnecting.
    /// </summary>
    AsyncEvent<BrokerClient> Disconnecting { get; }

    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TArg}" /> that is fired when the <see cref="DisconnectAsync" /> method has been called
    ///     and the client is disconnected.
    /// </summary>
    AsyncEvent<BrokerClient> Disconnected { get; }

    /// <summary>
    ///     Gets a value indicating whether the client is connected to the broker.
    /// </summary>
    ClientStatus Status { get; }

    /// <summary>
    ///     Initializes the connection to the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask ConnectAsync();

    /// <summary>
    ///     Disconnects from the message broker.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask DisconnectAsync();

    /// <summary>
    ///     Disconnects and reconnects the client.
    /// </summary>
    /// <remarks>
    ///     This is used to recover when the consumer is stuck in state where it's not able to rollback or commit anymore.
    /// </remarks>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask ReconnectAsync();
}
