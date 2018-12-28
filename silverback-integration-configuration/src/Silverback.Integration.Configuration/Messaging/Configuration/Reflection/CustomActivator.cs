// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Silverback.Messaging.Configuration.Reflection
{
    public class CustomActivator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TypeFinder _typeFinder;

        public CustomActivator(IServiceProvider serviceProvider, TypeFinder typeFinder)
        {
            _serviceProvider = serviceProvider;
            _typeFinder = typeFinder;
        }

        public T Activate<T>(string typeName, IConfigurationSection configSection)
        {
            var type = _typeFinder.FindClass(typeName);

            var constructorArgs = type
                .GetConstructors().First()
                .GetParameters()
                .Select(p => GetParameterValue(p, configSection, type))
                .ToArray();

            return (T) Activator.CreateInstance(type, constructorArgs);
        }

        private object GetParameterValue(ParameterInfo parameter, IConfigurationSection configSection, Type type)
        {
            var value = GetParameterValueFromConfig(parameter, configSection) ?? _serviceProvider.GetService(parameter.ParameterType);

            if (value == null)
            {
                throw new SilverbackConfigurationException(
                    $"Couldn't activate type {type.FullName}: " +
                    $"unable to find a value for the constructor parameter '{parameter.Name}' " +
                    $"neither in the configuration nor in the registered services.");
            }

            return value;
        }

        private object GetParameterValueFromConfig(ParameterInfo parameter, IConfigurationSection configSection)
        {
            var configValue = configSection.GetSection(parameter.Name).Value;

            if (string.IsNullOrEmpty(configValue))
                return null;

            return Convert.ChangeType(configValue, parameter.ParameterType);
        }
    }
}
