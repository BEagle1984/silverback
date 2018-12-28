// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Reflection;
using Silverback.Messaging.Serialization;

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

            SetSerializer(endpoint, configSection.GetSection("Serializer"));

            configSection.Bind(endpoint);

            return endpoint;
        }

        private void SetSerializer(IEndpoint endpoint, IConfigurationSection serializerConfigSection)
        {
            var serializerTypeName = serializerConfigSection.GetSection("Type").Value;

            if (string.IsNullOrEmpty(serializerTypeName))
                return;

            var endpointType = endpoint.GetType();
            var setMethod = endpointType.GetProperty("Serializer").GetSetMethod(true);

            if (setMethod == null)
                throw new InvalidOperationException($"The Serializer property on type {endpointType.FullName} doesn't have a setter.");

            setMethod.Invoke(
                endpoint, new object[]
                {
                    _customActivator.Activate<IMessageSerializer>(serializerTypeName, serializerConfigSection)
                });
        }
    }
}