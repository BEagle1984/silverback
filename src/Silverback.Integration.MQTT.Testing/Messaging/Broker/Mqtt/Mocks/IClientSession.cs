﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

/// <summary>
///     The session of a client connected to the <see cref="IInMemoryMqttBroker" />.
/// </summary>
public interface IClientSession
{
    /// <summary>
    ///     Gets the number of pending messages ready to be pushed to the client.
    /// </summary>
    int PendingMessagesCount { get; }
}
