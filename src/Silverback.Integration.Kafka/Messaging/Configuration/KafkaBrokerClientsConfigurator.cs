// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Configuration;

internal sealed class KafkaBrokerClientsConfigurator : IBrokerClientsConfigurator
{
    private readonly Action<KafkaClientsConfigurationBuilder> _configurationBuilderAction;

    public KafkaBrokerClientsConfigurator(Action<KafkaClientsConfigurationBuilder> configurationBuilderAction)
    {
        _configurationBuilderAction = configurationBuilderAction;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder) => builder.AddKafkaClients(_configurationBuilderAction);
}
