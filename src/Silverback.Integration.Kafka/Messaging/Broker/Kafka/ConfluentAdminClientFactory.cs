// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.AdminClientBuilder" />.
/// </summary>
public class ConfluentAdminClientFactory : IConfluentAdminClientFactory
{
    /// <inheritdoc cref="IConfluentAdminClientFactory.GetClient" />
    public IAdminClient GetClient(ClientConfig config) => new AdminClientBuilder(config).Build();
}
