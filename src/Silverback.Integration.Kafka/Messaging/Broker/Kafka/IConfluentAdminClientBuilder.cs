// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Used to build the <see cref="IAdminClient" />.
/// </summary>
public interface IConfluentAdminClientBuilder
{
    /// <summary>
    ///     Returns an <see cref="IAdminClient" />.
    /// </summary>
    /// <param name="config">
    ///     The client configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="IAdminClient" />.
    /// </returns>
    IAdminClient Build(ClientConfig config);
}
