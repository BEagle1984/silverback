// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Tools.Generators.Common;

public static class StringExtensions
{
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "False positive, it makes no sense")]
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length == 1)
            return value.ToLowerInvariant();

        return $"{value[..1].ToLowerInvariant()}{value[1..]}";
    }
}
