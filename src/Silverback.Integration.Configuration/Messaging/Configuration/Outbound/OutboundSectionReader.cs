// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Common;
using Silverback.Messaging.Configuration.Reflection;

namespace Silverback.Messaging.Configuration.Outbound
{
    public class OutboundSectionReader : InboundOutboundSectionBase
    {
        private readonly TypeFinder _typeFinder;

        public OutboundSectionReader(TypeFinder typeFinder, EndpointSectionReader endpointSectionReader)
            : base(typeFinder, endpointSectionReader)
        {
            _typeFinder = typeFinder;
        }

        public IEnumerable<ConfiguredOutbound> GetConfiguredOutbound(IConfigurationSection configSection) =>
            configSection.GetChildren().Select(GetConfiguredOutboundItem).ToList();

        private ConfiguredOutbound GetConfiguredOutboundItem(IConfigurationSection configSection)
        {
            try
            {
                return new ConfiguredOutbound(
                    GetMessageType(configSection),
                    GetConnectorType(configSection),
                    GetEndpoint<IProducerEndpoint>(configSection));
            }
            catch (Exception ex)
            {
                throw new SilverbackConfigurationException(
                    "Error in Outbound configuration section. See inner exception for details.", ex);
            }
        }

        protected Type GetMessageType(IConfigurationSection configSection)
        {
            var typeName = configSection.GetSection("MessageType").Value;

            if (typeName == null)
                return typeof(object);

            return _typeFinder.FindClassOrInterface(typeName);
        }
    }
}