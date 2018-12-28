// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Reflection;

namespace Silverback.Messaging.Configuration
{
    public class EndpointSectionReader
    {
        private readonly CustomActivator _customActivator;

        public EndpointSectionReader(CustomActivator customActivator)
        {
            _customActivator = customActivator;
        }

        public IEndpoint GetEndpoint(IConfigurationSection configSection)
        {
            var endpointTypeName = configSection.GetSection("Type").Value;

            if (string.IsNullOrWhiteSpace(endpointTypeName))
                throw new InvalidOperationException($"Missing Type in section {configSection.Path}.");

            var endpoint = _customActivator.Activate<IEndpoint>(endpointTypeName, configSection);

            configSection.Bind(endpoint);

            return endpoint;
        }
    }
}