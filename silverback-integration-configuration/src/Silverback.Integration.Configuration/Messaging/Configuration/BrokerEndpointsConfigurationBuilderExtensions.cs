// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Configuration;

namespace Silverback.Messaging.Configuration
{
    public static class BrokerEndpointsConfigurationBuilderExtensions
    {
        public static IBrokerEndpointsConfigurationBuilder ReadConfig(this IBrokerEndpointsConfigurationBuilder builder, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            return builder.ReadConfig(configuration.GetSection("Silverback:Endpoints"), serviceProvider);
        }

        public static IBrokerEndpointsConfigurationBuilder ReadConfig(this IBrokerEndpointsConfigurationBuilder builder, IConfigurationSection configurationSection, IServiceProvider serviceProvider)
        {
            new ConfigurationReader(serviceProvider).Read(configurationSection).Apply(builder);

            return builder;
        }
    }
}
