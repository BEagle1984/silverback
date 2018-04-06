using System;
using System.Collections.Concurrent;

namespace Silverback.Messaging
{
    /// <summary>
    /// Helps finding the <see cref="Type"/> from it's qualified name.
    /// </summary>
    internal static class ReflectionHelper
    {
        private static ConcurrentDictionary<string, Type> _types = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// Gets the <see cref="Type"/> corresponsind to the provided assembly qualified name.
        /// </summary>
        /// <param name="assemblyQualifiedName">The assembly qualified name.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Type GetType(string assemblyQualifiedName)
        {
            Type type;

            if (_types.TryGetValue(assemblyQualifiedName, out type))
            {
                return type;
            }

            type = Type.GetType(assemblyQualifiedName, false);

            if (type == null)
            {
                // Try with type and assembly name only
                type = Type.GetType(assemblyQualifiedName.Substring(0, assemblyQualifiedName.IndexOf(',', assemblyQualifiedName.IndexOf(',') + 1)), false);
            }

            if (type == null)
                throw new ArgumentException($"Couldn't load the type '{assemblyQualifiedName}.");

            _types.TryAdd(assemblyQualifiedName, type);

            return type;
        }
    }
}
