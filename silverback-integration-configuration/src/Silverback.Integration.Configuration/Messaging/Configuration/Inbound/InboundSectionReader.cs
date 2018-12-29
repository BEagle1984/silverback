// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Common;
using Silverback.Messaging.Configuration.Reflection;
using Silverback.Messaging.ErrorHandling;

namespace Silverback.Messaging.Configuration.Inbound
{
    public class InboundSectionReader : InboundOutboundSectionBase
    {
        private readonly ErrorPoliciesSectionReader _errorPoliciesSectionReader;

        public InboundSectionReader(TypeFinder typeFinder, EndpointSectionReader endpointSectionReader, ErrorPoliciesSectionReader errorPoliciesSectionReader)
            : base(typeFinder, endpointSectionReader)
        {
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

        private IEnumerable<ErrorPolicyBase> GetErrorPolicies(IConfigurationSection configSection) =>
            _errorPoliciesSectionReader.GetErrorPolicies(configSection.GetSection("ErrorPolicies"));
    }
}