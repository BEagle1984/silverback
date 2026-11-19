// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Silverback.Util;

// Inspired by EF Core: https://github.com/dotnet/efcore/blob/master/src/Shared/Check.cs
[DebuggerStepThrough]
internal static class Check
{
    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>(
        [NoEnumeration, ValidatedNotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static void NotNull<T>(
        [NoEnumeration, ValidatedNotNull] IAsyncEnumerable<T>? value,
        [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);
    }

    [ContractAnnotation("value:null => halt")]
    public static IReadOnlyCollection<T> NotEmpty<T>(
        [ValidatedNotNull] IReadOnlyCollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        value = NotNull(value, parameterName);

        if (value.Count == 0)
            throw new ArgumentException("Value cannot be an empty collection.", parameterName);

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotNullOrEmpty(
        [ValidatedNotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        if (value is null)
            throw new ArgumentNullException(parameterName);

        if (value.Trim().Length == 0)
            throw new ArgumentException("Value cannot be empty.", parameterName);

        return value;
    }

    public static string? NullButNotEmpty(string? value, [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        if (value is { Length: 0 })
            throw new ArgumentException("Value cannot be empty.", parameterName);

        return value;
    }

    public static IReadOnlyCollection<T> HasNoNulls<T>(
        IReadOnlyCollection<T?>? value,
        [CallerArgumentExpression(nameof(value))] string parameterName = "")
        where T : class
    {
        value = NotNull(value, parameterName);

        if (value.Any(element => element == null))
            throw new ArgumentException("The collection cannot contain null values.", parameterName);

        return (IReadOnlyCollection<T>)value;
    }

    public static IReadOnlyCollection<string?> HasNoEmpties(
        IReadOnlyCollection<string?>? value,
        [CallerArgumentExpression(nameof(value))] string parameterName = "")
    {
        value = NotNull(value, parameterName);

        if (value.Any(string.IsNullOrEmpty))
            throw new ArgumentException("The collection cannot contain null or empty values.", parameterName);

        return value;
    }

    public static T Range<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string parameterName = "")
        where T : struct, IComparable
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(parameterName, $"The value must be between {min} and {max}.");

        return value;
    }

    public static T GreaterThan<T>(T value, T min, [CallerArgumentExpression(nameof(value))] string parameterName = "")
        where T : struct, IComparable
    {
        if (value.CompareTo(min) <= 0)
            throw new ArgumentOutOfRangeException(parameterName, $"The value must be greater than {min}.");

        return value;
    }

    public static T? GreaterThan<T>(T? value, T min, [CallerArgumentExpression(nameof(value))] string parameterName = "")
        where T : struct, IComparable =>
        value.HasValue ? GreaterThan(value.Value, min, parameterName) : null;

    public static T GreaterOrEqualTo<T>(T value, T min, [CallerArgumentExpression(nameof(value))] string parameterName = "")
        where T : struct, IComparable
    {
        if (value.CompareTo(min) < 0)
            throw new ArgumentOutOfRangeException(parameterName, $"The value must be greater than or equal to {min}.");

        return value;
    }

    public static void ThrowObjectDisposedIf(bool condition, object instance) =>
        ObjectDisposedException.ThrowIf(condition, instance);

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class ValidatedNotNullAttribute : Attribute;
}
