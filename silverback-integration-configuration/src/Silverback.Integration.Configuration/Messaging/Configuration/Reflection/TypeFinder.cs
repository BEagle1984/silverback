// Copyright (c) 2018 Sergio Aquilini
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

        public Type FindClass(string className)
        {
            var type = _assemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.IsClass && t.IsPublic &&
                    (t.FullName == className || t.FullName.EndsWith("." + className)));

            if (type == null)
                throw new SilverbackConfigurationException($"Couldn't find a public class {className}. Maybe an assembly is missing in the Using configuration section.");

            return type;
        }
    }
}