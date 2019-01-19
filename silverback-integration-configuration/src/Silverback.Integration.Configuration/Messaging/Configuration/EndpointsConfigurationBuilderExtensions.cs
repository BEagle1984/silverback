// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Configuration;

namespace Silverback.Messaging.Configuration
{
    public static class EndpointsConfigurationBuilderExtensions
    {
        public static EndpointsConfigurationBuilder ReadConfig(this EndpointsConfigurationBuilder builder, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            return builder.ReadConfig(configuration.GetSection("Silverback"), serviceProvider);
        }

        public static EndpointsConfigurationBuilder ReadConfig(this EndpointsConfigurationBuilder builder, IConfigurationSection configurationSection, IServiceProvider serviceProvider)
        {
            new ConfigurationReader(serviceProvider).Read(configurationSection).Apply(builder);

            return builder;
        }
    }
}
