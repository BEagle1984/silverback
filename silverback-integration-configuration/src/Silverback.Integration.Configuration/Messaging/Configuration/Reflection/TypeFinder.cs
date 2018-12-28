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

        public Type FindClass(string className) => FindClass(new [] {className});

        public Type FindClass(params string[] classNames)
        {
            var type = _assemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.IsClass && t.IsPublic &&
                    classNames.Any(name => t.FullName == name || t.FullName.EndsWith("." + name)));

            if (type == null)
                throw new SilverbackConfigurationException($"Couldn't find a public class named {string.Join(" or ", classNames)}. Maybe an assembly is missing in the Using configuration section.");

            return type;
        }
    }
}