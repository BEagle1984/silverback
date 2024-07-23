// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The factory used to create <see cref="IAdminClient" /> instances.
/// </summary>
public interface IConfluentAdminClientFactory
{
    /// <summary>
    ///     Returns an <see cref="IAdminClient" /> instance for the specified configuration.
    /// </summary>
    /// <param name="config">
    ///     The client configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient GetClient(ClientConfig config);
}
