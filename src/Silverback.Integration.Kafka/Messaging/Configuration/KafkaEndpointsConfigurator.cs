// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Configuration;

internal sealed class KafkaEndpointsConfigurator : IEndpointsConfigurator
{
    private readonly Action<KafkaEndpointsConfigurationBuilder> _configurationBuilderAction;

    public KafkaEndpointsConfigurator(Action<KafkaEndpointsConfigurationBuilder> configurationBuilderAction)
    {
        _configurationBuilderAction = configurationBuilderAction;
    }

    public void Configure(EndpointsConfigurationBuilder builder) => builder.AddKafkaEndpoints(_configurationBuilderAction);
}
