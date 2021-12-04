// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;

namespace Silverback.Util;

internal static class CloneExtensions
{
    public static T ShallowCopy<T>(this T source)
        where T : class
    {
        Check.NotNull(source, nameof(source));

        MethodInfo? memberwiseCloneMethodInfo = source.GetType().GetMethod(
            "MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (memberwiseCloneMethodInfo == null)
            throw new InvalidOperationException("MemberwiseClone method not found.");

        return (T)memberwiseCloneMethodInfo.Invoke(source, null)!;
    }
}
