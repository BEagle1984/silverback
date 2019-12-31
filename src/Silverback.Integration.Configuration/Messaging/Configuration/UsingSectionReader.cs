// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Configuration
{
    public class UsingSectionReader
    {
        public IEnumerable<Assembly> GetAssemblies(IConfigurationSection configSection)
        {
            var assemblies = new Dictionary<string, Assembly>();

            try
            {
                AddAssembliesFromUsingSection(configSection, assemblies);

                // Scan Silverback.Integration per default
                AddAssemblyContaining(typeof(IBroker), assemblies);
            }
            catch (Exception ex)
            {
                throw new SilverbackConfigurationException(
                    "Error in Using configuration section. See inner exception for details.", ex);
            }

            return assemblies.Select(a => a.Value).ToList();
        }

        private static void AddAssemblyContaining(Type containedType, IDictionary<string, Assembly> assemblies)
        {
            var assembly = Assembly.GetAssembly(containedType);
            if (!assemblies.ContainsKey(assembly.FullName))
                assemblies.Add(assembly.FullName, assembly);
        }

        private void AddAssembliesFromUsingSection(
            IConfigurationSection configSection,
            IDictionary<string, Assembly> assemblies)
        {
            foreach (var assemblyName in configSection.GetChildren().Select(c => c.Value))
            {
                var assembly = Assembly.Load(new AssemblyName(assemblyName));
                if (!assemblies.ContainsKey(assembly.FullName))
                    assemblies.Add(assembly.FullName, assembly);
            }
        }
    }
}