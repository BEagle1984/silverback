// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Util;

// TODO: Test
internal static class ReflectionCloneExtensions
{
    public static T ShallowCopy<T>(this T source)
        where T : new()
    {
        T cloned = new();
        IEnumerable<PropertyInfo> properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property is { CanRead: true, CanWrite: true });

        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(source);

            if (!Equals(value, property.PropertyType.GetDefaultValue()))
                property.SetValue(cloned, value);
        }

        return cloned;
    }
}
