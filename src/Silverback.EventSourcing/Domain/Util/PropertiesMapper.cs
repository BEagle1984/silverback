// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Domain.Util
{
    internal static class PropertiesMapper
    {
        public static void Map(object source, object destination)
        {
            var sourceProperties = source.GetType().GetProperties();
            var destProperties = destination.GetType().GetProperties();
            var destTypeName = destination.GetType().Name;

            foreach (var sourceProperty in sourceProperties)
            {
                if (TryMapProperty(source, destination, sourceProperty, destProperties, sourceProperty.Name))
                    continue;

                if (sourceProperty.Name.StartsWith(destTypeName))
                    if (TryMapProperty(source, destination, sourceProperty, destProperties, sourceProperty.Name.Substring(destTypeName.Length)))
                        continue;

                if (sourceProperty.Name.StartsWith("Entity"))
                    TryMapProperty(source, destination, sourceProperty, destProperties, sourceProperty.Name.Substring("Entity".Length));
            }
        }

        private static bool TryMapProperty(object source, object destination, PropertyInfo sourcePropertyInfo,
            IEnumerable<PropertyInfo> destPropertiesInfo, string destPropertyName)
        {
            var destPropertyInfo = destPropertiesInfo.SingleOrDefault(p => p.Name == destPropertyName);
            if (destPropertyInfo == null)
                return false;

            var setterMethod = destPropertyInfo.GetSetMethod(true);
            if (setterMethod == null)
                return false;

            try
            {
                setterMethod.Invoke(destination, new[] {sourcePropertyInfo.GetValue(source)});
                return true;
            }
            catch (Exception ex)
            {
                throw new SilverbackException(
                    $"Couldn't map property {sourcePropertyInfo.DeclaringType.Name}.{sourcePropertyInfo.Name} " +
                    $"to  {destPropertyInfo.DeclaringType.Name}.{destPropertyInfo.Name}." +
                    "See inner exception for details.", ex);
            }
        }
    }
}