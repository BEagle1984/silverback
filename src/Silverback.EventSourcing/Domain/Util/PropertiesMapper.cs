﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Domain.Util;

internal static class PropertiesMapper
{
    public static void Map(object source, object destination)
    {
        PropertyInfo[] sourceProperties = source.GetType().GetProperties();
        PropertyInfo[] destProperties = destination.GetType().GetProperties();
        string destTypeName = destination.GetType().Name;

        foreach (PropertyInfo sourceProperty in sourceProperties)
        {
            if (TryMapProperty(source, destination, sourceProperty, destProperties, sourceProperty.Name))
                continue;

            if (sourceProperty.Name.StartsWith(destTypeName, StringComparison.InvariantCulture) &&
                TryMapProperty(
                    source,
                    destination,
                    sourceProperty,
                    destProperties,
                    sourceProperty.Name.Substring(destTypeName.Length)))
            {
                continue;
            }

            if (sourceProperty.Name.StartsWith("Entity", StringComparison.InvariantCulture))
            {
                TryMapProperty(
                    source,
                    destination,
                    sourceProperty,
                    destProperties,
                    sourceProperty.Name.Substring("Entity".Length));
            }
        }
    }

    private static bool TryMapProperty(
        object source,
        object destination,
        PropertyInfo sourcePropertyInfo,
        IEnumerable<PropertyInfo> destPropertiesInfo,
        string destPropertyName)
    {
        PropertyInfo? destPropertyInfo = destPropertiesInfo.SingleOrDefault(p => p.Name == destPropertyName);
        if (destPropertyInfo == null)
            return false;

        MethodInfo? setterMethod = destPropertyInfo.GetSetMethod(true);
        if (setterMethod == null)
            return false;

        try
        {
            setterMethod.Invoke(destination, new[] { sourcePropertyInfo.GetValue(source) });
            return true;
        }
        catch (Exception ex)
        {
            throw new EventSourcingException(
                $"Couldn't map property {sourcePropertyInfo.DeclaringType?.Name}.{sourcePropertyInfo.Name} " +
                $"to  {destPropertyInfo.DeclaringType?.Name}.{destPropertyInfo.Name}." +
                "See inner exception for details.",
                ex);
        }
    }
}
