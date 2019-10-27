// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Common;
using Silverback.Messaging.Configuration.Inbound;
using Silverback.Messaging.Configuration.Outbound;
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
        public ICollection<ConfiguredOutbound> Outbound { get; private set; }

        public ConfigurationReader Read(IConfigurationSection configSection)
        {
            var assemblies = new UsingSectionReader().GetAssemblies(configSection.GetSection("Using"));
            var typeFinder = new TypeFinder(assemblies);
            var customActivator = new CustomActivator(_serviceProvider, typeFinder);

            var endpointSectionReader = new EndpointSectionReader(customActivator);
            var errorPoliciesSectionReader = new ErrorPoliciesSectionReader(customActivator);
            var inboundSettingsSectionReader = new InboundSettingsSectionReader();
            var inboundSectionReader = new InboundSectionReader(typeFinder, endpointSectionReader, errorPoliciesSectionReader, inboundSettingsSectionReader);
            var outboundSectionReader = new OutboundSectionReader(typeFinder, endpointSectionReader);

            Inbound = inboundSectionReader
                .GetConfiguredInbound(configSection.GetSection("Inbound"))
                .ToList();

            Outbound = outboundSectionReader
                .GetConfiguredOutbound(configSection.GetSection("Outbound"))
                .ToList();

            return this;
        }
        
        public void Apply(IEndpointsConfigurationBuilder builder)
        {
            foreach (var inbound in Inbound)
            {
                builder.AddInbound(inbound.Endpoint, inbound.ConnectorType, b => inbound.ErrorPolicies.Any() ? b.Chain(inbound.ErrorPolicies) : null);
            }

            foreach (var outbound in Outbound)
            {
                builder.AddOutbound(outbound.MessageType, outbound.Endpoint, outbound.ConnectorType);
            }
        }
    }
}