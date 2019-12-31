// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Reflection;

namespace Silverback.Messaging.Configuration.Common
{
    public class InboundOutboundSectionBase
    {
        private readonly TypeFinder _typeFinder;
        private readonly EndpointSectionReader _endpointSectionReader;

        public InboundOutboundSectionBase(TypeFinder typeFinder, EndpointSectionReader endpointSectionReader)
        {
            _typeFinder = typeFinder;
            _endpointSectionReader = endpointSectionReader;
        }

        protected Type GetConnectorType(IConfigurationSection configSection)
        {
            var typeName = configSection.GetSection("ConnectorType").Value;

            if (typeName == null)
                return null;

            return _typeFinder.FindClass(typeName);
        }

        protected TEndpoint GetEndpoint<TEndpoint>(IConfigurationSection configSection)
            where TEndpoint : IEndpoint
        {
            var endpointConfig = configSection.GetSection("Endpoint");

            if (!endpointConfig.Exists())
                throw new InvalidOperationException($"Missing Endpoint in section {configSection.Path}.");

            return (TEndpoint) _endpointSectionReader.GetEndpoint(endpointConfig);
        }
    }
}