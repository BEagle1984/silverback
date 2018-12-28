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
        private readonly TypeFinder _typeFinder;
        private readonly EndpointSectionReader _endpointSectionReader;
        private readonly ErrorPoliciesSectionReader _errorPoliciesSectionReader;

        public InboundSectionReader(TypeFinder typeFinder, EndpointSectionReader endpointSectionReader, ErrorPoliciesSectionReader errorPoliciesSectionReader)
        {
            _typeFinder = typeFinder;
            _endpointSectionReader = endpointSectionReader;
            _errorPoliciesSectionReader = errorPoliciesSectionReader;
        }

        public IEnumerable<ConfiguredInbound> GetConfiguredInbound(IConfigurationSection configSection) =>
            configSection.GetChildren().Select(GetConfiguredInboundItem).ToList();

        private ConfiguredInbound GetConfiguredInboundItem(IConfigurationSection configSection)
        {
            try
            {
                return new ConfiguredInbound(
                    GetConnectorType(configSection),
                    GetEndpoint(configSection),
                    GetErrorPolicies(configSection));
            }
            catch (Exception ex)
            {
                throw new SilverbackConfigurationException("Error in Inbound configuration section. See inner exception for details.", ex);
            }
        }

        private Type GetConnectorType(IConfigurationSection configSection)
        {
            var typeName = configSection.GetSection("ConnectorType").Value;

            if (typeName == null)
                return null;

            return _typeFinder.FindClass(typeName);
        }

        private IEndpoint GetEndpoint(IConfigurationSection configSection)
        {
            var endpointConfig = configSection.GetSection("Endpoint");

            if (!endpointConfig.Exists())
                throw new InvalidOperationException($"Missing Endpoint in section {configSection.Path}.");

            return _endpointSectionReader.GetEndpoint(endpointConfig);
        }

        private IEnumerable<ErrorPolicyBase> GetErrorPolicies(IConfigurationSection configSection) =>
            _errorPoliciesSectionReader.GetErrorPolicies(configSection.GetSection("ErrorPolicies"));
    }
}