// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Configuration
{
    internal sealed class KafkaEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly Action<IKafkaEndpointsConfigurationBuilder> _configAction;

        public KafkaEndpointsConfigurator(Action<IKafkaEndpointsConfigurationBuilder> configAction)
        {
            _configAction = configAction;
        }

        public void Configure(IEndpointsConfigurationBuilder builder) =>
            builder.AddKafkaEndpoints(_configAction);
    }
}
