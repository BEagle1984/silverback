// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Messaging.Configuration.Reflection
{
    public class TypeFinder
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public TypeFinder(IEnumerable<Assembly> assemblies)
        {
            _assemblies = assemblies;
        }

        public Type FindClass(params string[] classNames)
        {
            var type = FindType(t => t.IsClass && t.IsPublic, classNames);

            if (type == null)
                throw new SilverbackConfigurationException($"Couldn't find a public class named {string.Join(" or ", classNames)}. Maybe an assembly is missing in the Using configuration section.");

            return type;
        }

        public Type FindInterface(params string[] interfaceNames)
        {
            var type = FindType(t => t.IsInterface && t.IsPublic, interfaceNames);

            if (type == null)
                throw new SilverbackConfigurationException($"Couldn't find a public interface named {string.Join(" or ", interfaceNames)}. Maybe an assembly is missing in the Using configuration section.");

            return type;
        }
        
        public Type FindClassOrInterface(params string[] typeNames)
        {
            var type = FindType(t => t.IsPublic, typeNames);

            if (type == null)
                throw new SilverbackConfigurationException($"Couldn't find a public class or interface named {string.Join(" or ", typeNames)}. Maybe an assembly is missing in the Using configuration section.");

            return type;
        }

        private Type FindType(Func<Type, bool> predicate, params string[] typeNames) =>
            _assemblies
                .SelectMany(a => a.GetTypes())
                .Where(predicate)
                .FirstOrDefault(t =>
                    typeNames.Any(name => t.FullName == name || t.FullName.EndsWith("." + name)));
    }
}