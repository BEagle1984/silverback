// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka
{
    /// <summary>
    ///     Wraps the <see cref="Confluent.Kafka.AdminClientBuilder" />.
    /// </summary>
    public class ConfluentAdminClientBuilder : IConfluentAdminClientBuilder
    {
        /// <inheritdoc cref="IConfluentAdminClientBuilder.Build" />
        public IAdminClient Build(ClientConfig config) => new AdminClientBuilder(config).Build();
    }
}
