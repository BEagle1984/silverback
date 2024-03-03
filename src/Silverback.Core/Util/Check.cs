// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace Silverback.Util;

// Inspired by EF Core: https://github.com/dotnet/efcore/blob/master/src/Shared/Check.cs
[DebuggerStepThrough]
internal static class Check
{
    [ContractAnnotation("value:null => halt")]
    public static T NotNull<T>([NoEnumeration, ValidatedNotNull] T? value, [InvokerParameterName] string parameterName)
    {
        if (value is null)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static void NotNull<T>([NoEnumeration, ValidatedNotNull] IAsyncEnumerable<T>? value, [InvokerParameterName] string parameterName)
    {
        if (value is null)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));
            throw new ArgumentNullException(parameterName);
        }
    }

    [ContractAnnotation("value:null => halt")]
    public static IReadOnlyCollection<T> NotEmpty<T>([ValidatedNotNull] IReadOnlyCollection<T>? value, [InvokerParameterName] string parameterName)
    {
        value = NotNull(value, parameterName);

        if (value.Count == 0)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));
            throw new ArgumentException("Value cannot be an empty collection.", parameterName);
        }

        return value;
    }

    [ContractAnnotation("value:null => halt")]
    public static string NotNullOrEmpty(
        [ValidatedNotNull] string? value,
        [InvokerParameterName] string parameterName)
    {
        Exception? exception = null;

        if (value is null)
            exception = new ArgumentNullException(parameterName);
        else if (value.Trim().Length == 0)
            exception = new ArgumentException("Value cannot be empty.", parameterName);

        if (exception != null)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw exception;
        }

        return value!;
    }

    public static string? NullButNotEmpty(string? value, [InvokerParameterName] string parameterName)
    {
        if (value != null && value.Length == 0)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value;
    }

    public static IReadOnlyCollection<T> HasNoNulls<T>(
        IReadOnlyCollection<T?>? value,
        [InvokerParameterName] string parameterName)
        where T : class
    {
        value = NotNull(value, parameterName);

        if (value.Any(element => element == null))
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw new ArgumentException("The collection cannot contain null values.", parameterName);
        }

        return (IReadOnlyCollection<T>)value;
    }

    public static IReadOnlyCollection<string?> HasNoEmpties(
        IReadOnlyCollection<string?>? value,
        [InvokerParameterName] string parameterName)
    {
        value = NotNull(value, parameterName);

        if (value.Any(string.IsNullOrEmpty))
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw new ArgumentException("The collection cannot contain null or empty values.", parameterName);
        }

        return value;
    }

    public static T Range<T>(T value, string parameterName, T min, T max)
        where T : struct, IComparable
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw new ArgumentOutOfRangeException(parameterName, $"The value must be between {min} and {max}.");
        }

        return value;
    }

    public static T GreaterThan<T>(T value, string parameterName, T min)
        where T : struct, IComparable
    {
        if (value.CompareTo(min) <= 0)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw new ArgumentOutOfRangeException(parameterName, $"The value must be greater than {min}.");
        }

        return value;
    }

    public static T GreaterOrEqualTo<T>(T value, string parameterName, T min)
        where T : struct, IComparable
    {
        if (value.CompareTo(min) < 0)
        {
            NotNullOrEmpty(parameterName, nameof(parameterName));

            throw new ArgumentOutOfRangeException(parameterName, $"The value must be greater or equal to {min}.");
        }

        return value;
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    private sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}
