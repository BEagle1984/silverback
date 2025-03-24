// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Initializes the broker clients, producers and consumers.
/// </summary>
public interface IBrokerClientsInitializer
{
    /// <summary>
    ///     Initializes the broker clients, producers and consumers.
    /// </summary>
    void Initialize();
}
