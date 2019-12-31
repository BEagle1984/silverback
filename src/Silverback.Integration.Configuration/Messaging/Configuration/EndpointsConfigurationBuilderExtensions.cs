// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Configuration;

namespace Silverback.Messaging.Configuration
{
    public static class EndpointsConfigurationBuilderExtensions
    {
        public static IEndpointsConfigurationBuilder ReadConfig(
            this IEndpointsConfigurationBuilder builder,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            return builder.ReadConfig(configuration.GetSection("Silverback"), serviceProvider);
        }

        public static IEndpointsConfigurationBuilder ReadConfig(
            this IEndpointsConfigurationBuilder builder,
            IConfigurationSection configurationSection,
            IServiceProvider serviceProvider)
        {
            new ConfigurationReader(serviceProvider).Read(configurationSection).Apply(builder);

            return builder;
        }
    }
}