// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Reflection;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    public class InboundSectionReader
    {
        private readonly IConfigurationSection _configSection;
        private readonly TypeFinder _typeFinder;
        private readonly EndpointSectionReader _endpointSectionReader;

        public InboundSectionReader(IConfigurationSection configSection, TypeFinder typeFinder, EndpointSectionReader endpointSectionReader)
        {
            _configSection = configSection;
            _typeFinder = typeFinder;
            _endpointSectionReader = endpointSectionReader;
        }

        public IEnumerable<ConfiguredInbound> GetConfiguredInbound() =>
            _configSection.GetChildren().Select(GetConfiguredInbound).ToList();

        public ConfiguredInbound GetConfiguredInbound(IConfigurationSection itemConfigSection)
        {
            try
            {
                return new ConfiguredInbound(
                    GetConnectorType(itemConfigSection),
                    GetEndpoint(itemConfigSection),
                    GetErrorPolicies(itemConfigSection));
            }
            catch (Exception ex)
            {
                throw new SilverbackConfigurationException("Error in Inbound configuration section. See inner exception for details.", ex);
            }
        }

        private Type GetConnectorType(IConfigurationSection itemConfigSection)
        {
            var typeName = itemConfigSection.GetSection("ConnectorType").Value;

            if (typeName == null)
                return null;

            return _typeFinder.FindClass(typeName);
        }

        private IEndpoint GetEndpoint(IConfigurationSection itemConfigSection)
        {
            var endpointConfig = itemConfigSection.GetSection("Endpoint");

            if (!endpointConfig.Exists())
                throw new InvalidOperationException($"Missing Endpoint in section {itemConfigSection.Path}.");

            return _endpointSectionReader.GetEndpoint(endpointConfig);
        }

        private IEnumerable<ErrorPolicyBase> GetErrorPolicies(IConfigurationSection itemConfigSection)
        {
            // TODO: Implement
            return Enumerable.Empty<ErrorPolicyBase>();
        }
    }
}