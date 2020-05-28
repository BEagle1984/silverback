// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;

namespace Silverback.Util
{
    // Inspired by EF Core: https://github.com/dotnet/efcore/blob/master/src/Shared/Check.cs
    [DebuggerStepThrough]
    internal static class Check
    {
        [ContractAnnotation("value:null => halt")]
        public static T NotNull<T>(
            [NoEnumeration] [ValidatedNotNullAttribute]
            T? value,
            [InvokerParameterName] string parameterName)
            where T : class
        {
            if (ReferenceEquals(value, null))
            {
                NotEmpty(parameterName, nameof(parameterName));
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        [ContractAnnotation("value:null => halt")]
        public static IReadOnlyCollection<T> NotEmpty<T>(
            [ValidatedNotNullAttribute] IReadOnlyCollection<T>? value,
            [InvokerParameterName] string parameterName)
        {
            value = NotNull(value, parameterName);

            if (value.Count == 0)
            {
                NotEmpty(parameterName, nameof(parameterName));
                throw new ArgumentException("Value cannot be an empty collection.");
            }

            return value;
        }

        [ContractAnnotation("value:null => halt")]
        public static string NotEmpty(
            string? value,
            [InvokerParameterName] string parameterName)
        {
            Exception? exception = null;

            if (value is null)
                exception = new ArgumentNullException(parameterName);
            else if (value.Trim().Length == 0)
                exception = new ArgumentException("Value cannot be empty.");

            if (exception != null)
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw exception;
            }

            return value!;
        }

        public static string? NullButNotEmpty(string? value, [InvokerParameterName] string parameterName)
        {
            if (!(value is null) && value.Length == 0)
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentException("Value cannot be empty.", parameterName);
            }

            return value;
        }

        public static IReadOnlyCollection<T> HasNoNulls<T>(
            IReadOnlyCollection<T>? value,
            [InvokerParameterName] [System.Diagnostics.CodeAnalysis.NotNull]
            string parameterName)
            where T : class
        {
            value = NotNull(value, parameterName);

            if (value.Any(element => element == null))
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentException("The collection cannot contain null values", parameterName);
            }

            return value;
        }

        // TODO: Needed? Used?
        [Conditional("DEBUG")]
        public static void DebugAssert([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException($"Check.DebugAssert failed: {message}");
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        private sealed class ValidatedNotNullAttribute : Attribute
        {
        }
    }
}
