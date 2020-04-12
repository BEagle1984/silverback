// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util
{
    internal static class TypeExtensions
    {
        public static object GetDefaultValue(this Type type) =>
            type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}