// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Configuration.Inbound
{
    public class InboundSettingsSectionReader
    {
        public InboundConnectorSettings GetSettings(IConfigurationSection configSection)
        {
            if (!configSection.Exists())
                return null;

            var settings = new InboundConnectorSettings();
            configSection.Bind(settings);
            return settings;
        }
    }
}