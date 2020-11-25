// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Silverback.Util
{
    /// <summary>
    ///     Adds some extension methods to the <see cref="IEnumerable{T}" /> that gets the target type as
    ///     <see cref="Type" /> instead of generic type parameter.
    /// </summary>
    internal static class EnumerableTypeParameterExtensions
    {
        public static IEnumerable<object> OfType(this IEnumerable<object> source, Type type) =>
            Invoke(source, type, "OfType");

        public static IEnumerable<object> Cast(this IEnumerable<object> source, Type type) =>
            Invoke(source, type, "Cast");

        public static IEnumerable<object> ToList(this IEnumerable<object> source, Type type) =>
            Invoke(source, type, "ToList");

        private static IEnumerable<object> Invoke(IEnumerable<object> source, Type type, string methodName)
        {
            return typeof(Enumerable)
                    .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)
                    ?.MakeGenericMethod(type)
                    .Invoke(null, new object[] { source })
                as IEnumerable<object> ?? Enumerable.Empty<object>();
        }
    }
}
