// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Configuration.Common;

namespace Silverback.Messaging.Configuration.Reflection
{
    public class CustomActivator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TypeFinder _typeFinder;
        private readonly EndpointSectionReader _endpointSectionReader;

        public CustomActivator(IServiceProvider serviceProvider, TypeFinder typeFinder)
        {
            _serviceProvider = serviceProvider;
            _typeFinder = typeFinder;

            _endpointSectionReader = new EndpointSectionReader(this);
        }

        public T Activate<T>(string typeName, IConfigurationSection configSection) =>
            Activate<T>(configSection, typeName);

        public T Activate<T>(IConfigurationSection configSection, params string[] typeNames)
        {
            var type = _typeFinder.FindClass(typeNames);

            var constructorArgs = type
                .GetConstructors().First()
                .GetParameters()
                .Select(p => GetParameterValue(p, configSection, type))
                .ToArray();

            return (T) Activator.CreateInstance(type, constructorArgs);
        }

        private object GetParameterValue(ParameterInfo parameter, IConfigurationSection configSection, Type type)
        {
            var value = GetParameterValueFromConfig(parameter, configSection) ??
                        _serviceProvider.GetService(parameter.ParameterType);

            if (value == null)
            {
                if (parameter.HasDefaultValue)
                    return null;

                throw new SilverbackConfigurationException(
                    $"Couldn't activate type {type.FullName}: " +
                    $"unable to find a value for the constructor parameter '{parameter.Name}' " +
                    "neither in the configuration nor in the registered services.");
            }

            return value;
        }

        private object GetParameterValueFromConfig(ParameterInfo parameter, IConfigurationSection configSection)
        {
            switch (parameter.Name.ToLowerInvariant())
            {
                case "endpoint":
                    return _endpointSectionReader.GetEndpoint(configSection.GetSection("Endpoint"));
                case "names":
                    return GetNames(configSection);
            }

            var configValue = configSection.GetSection(parameter.Name).Value;

            if (string.IsNullOrEmpty(configValue))
                return null;

            return Convert(configValue, parameter.ParameterType);
        }

        private object GetNames(IConfigurationSection configSection)
        {
            var namesSection = configSection.GetSection("Names");

            if (namesSection.Exists())
            {
                return namesSection.GetChildren().Select(c => c.Value).ToArray();
            }

            return configSection.GetSection("Name").Value;
        }

        private object Convert(object value, Type targetType)
        {
            var converter = TypeDescriptor.GetConverter(targetType);
            return converter.ConvertFrom(value);
        }
    }
}