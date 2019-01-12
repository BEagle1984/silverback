// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Util
{
    // TOOD: Test
    internal static class EnumerableOfTypeExtensions
    {
        public static IEnumerable<object> OfType(this IEnumerable<object> source, Type type) =>
            (IEnumerable<object>)
            typeof(Enumerable).GetMethod("OfType", BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(type)
                .Invoke(null, new object[] { source });
    }
}