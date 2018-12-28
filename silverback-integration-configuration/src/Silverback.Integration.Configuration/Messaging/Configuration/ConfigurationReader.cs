// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Reflection;

namespace Silverback.Messaging.Configuration
{
    public class ConfigurationReader
    {
        private readonly IServiceProvider _serviceProvider;

        public ConfigurationReader(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICollection<ConfiguredInbound> Inbound { get; private set; }

        public ConfigurationReader Read(IConfigurationSection configSection)
        {
            var typeFinder = new TypeFinder(new UsingSectionReader(configSection.GetSection("Using")).GetAssemblies());
            var customActivator = new CustomActivator(_serviceProvider, typeFinder);
            var endpointSectionReader = new EndpointSectionReader(customActivator);

            Inbound =
                new InboundSectionReader(configSection.GetSection("Inbound"), typeFinder, endpointSectionReader)
                    .GetConfiguredInbound()
                    .ToList();

            return this;
        }
        
        public void Configure(IBrokerEndpointsConfigurationBuilder builder)
        {
            foreach (var inbound in Inbound)
            {
                builder.AddInbound(inbound.Endpoint, inbound.ConnectorType, b => inbound.ErrorPolicies != null ? b.Chain(inbound.ErrorPolicies) : null);
            }
        }
    }
}