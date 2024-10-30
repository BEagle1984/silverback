// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Add some helper methods to the <see cref="IEnumerable{T}" /> of <see cref="MessageHeader" />.
/// </summary>
public static class MessageHeaderEnumerableExtensions
{
    /// <summary>
    ///     Returns a boolean value indicating whether an header with the specified name has already been added
    ///     to the collection.
    /// </summary>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name to be checked.
    /// </param>
    /// <returns>
    ///     A boolean value indicating whether the name was found in the existing headers.
    /// </returns>
    public static bool Contains(this IEnumerable<MessageHeader> headers, string name) =>
        headers.Any(header => header.Name == name);

    /// <summary>
    ///     Checks whether an header with the specified name exists and returns its value.
    /// </summary>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name of the header to be retrieved.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    /// <returns>
    ///     A value indicating whether the header was found.
    /// </returns>
    public static bool TryGetValue(this IEnumerable<MessageHeader> headers, string name, out string? value)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNullOrEmpty(name, nameof(name));

        MessageHeader? header = headers.FirstOrDefault(header => header.Name == name);

        if (header == null)
        {
            value = null;
            return false;
        }

        value = header.Value;
        return true;
    }

    /// <summary>
    ///     <para>
    ///         Returns the value of the header with the specified name.
    ///     </para>
    ///     <para>
    ///         By default, it will return <c>null</c> if no header with that name is found in the collection but
    ///         this behavior can be changed setting the <paramref name="throwIfNotFound" /> parameter to <c>true</c>.
    ///     </para>
    /// </summary>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name of the header to be retrieved.
    /// </param>
    /// <param name="throwIfNotFound">
    ///     A boolean value specifying whether an exception must be thrown if the header with the specified name
    ///     is not found (or the value cannot be converted to the specified type).
    /// </param>
    /// <returns>
    ///     The header value converted to the target type, or <c>null</c> if not found.
    /// </returns>
    public static string? GetValue(
        this IEnumerable<MessageHeader> headers,
        string name,
        bool throwIfNotFound = false) =>
        (string?)headers.GetValue(name, typeof(string), throwIfNotFound);

    /// <summary>
    ///     <para>
    ///         Returns the value of the header with the specified name, casting it to the specified type
    ///         <typeparamref name="T" />.
    ///     </para>
    ///     <para>
    ///         By default, it will return <c>null</c> if no header with that name is found in the collection but
    ///         this behavior can be changed setting the <paramref name="throwIfNotFound" /> parameter to <c>true</c>.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     The type to convert the header value to.
    /// </typeparam>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name of the header to be retrieved.
    /// </param>
    /// <param name="throwIfNotFound">
    ///     A boolean value specifying whether an exception must be thrown if the header with the specified name
    ///     is not found (or the value cannot be converted to the specified type).
    /// </param>
    /// <returns>
    ///     The header value converted to the target type, or <c>null</c> if not found.
    /// </returns>
    public static T? GetValue<T>(this IEnumerable<MessageHeader> headers, string name, bool throwIfNotFound = false)
        where T : struct =>
        (T?)headers.GetValue(name, typeof(T), throwIfNotFound);

    /// <summary>
    ///     <para>
    ///         Returns the value of the header with the specified name, casting it to the specified type.
    ///     </para>
    ///     <para>
    ///         By default, it will return <c>null</c> if no header with that name is found in the collection but
    ///         this behavior can be changed setting the <paramref name="throwIfNotFound" /> parameter to <c>true</c>.
    ///     </para>
    /// </summary>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name of the header to be retrieved.
    /// </param>
    /// <param name="targetType">
    ///     The type to convert the header value to.
    /// </param>
    /// <param name="throwIfNotFound">
    ///     A boolean value specifying whether an exception must be thrown if the header with the specified name
    ///     is not found (or the value cannot be converted to the specified type).
    /// </param>
    /// <returns>
    ///     The header value converted to the target type, or <c>null</c> if not found.
    /// </returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rethrown if desired")]
    public static object? GetValue(
        this IEnumerable<MessageHeader> headers,
        string name,
        Type targetType,
        bool throwIfNotFound = false)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(targetType, nameof(targetType));

        bool found = headers.TryGetValue(name, out string? value);

        if (!found)
        {
            if (throwIfNotFound)
                throw new ArgumentOutOfRangeException(nameof(name), $"No '{name}' header was found.");

            return null;
        }

        if (targetType == typeof(string))
            return value;

        try
        {
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            if (throwIfNotFound)
                throw new InvalidOperationException($"The '{name}' header cannot be converted to {targetType.Name}.", ex);

            return null;
        }
    }

    /// <summary>
    ///     <para>
    ///         Returns the value of the header with the specified name, casting it to the specified type
    ///         <typeparamref name="T" />.
    ///     </para>
    ///     <para>
    ///         It will return the default value for the type <typeparamref name="T" /> if no header with that
    ///         name is found in the collection.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     The type to convert the header value to.
    /// </typeparam>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name of the header to be retrieved.
    /// </param>
    /// <returns>
    ///     The header value converted to the target type, or <c>null</c> if not found.
    /// </returns>
    public static T GetValueOrDefault<T>(this IEnumerable<MessageHeader> headers, string name)
        where T : struct =>
        GetValue<T>(headers, name) ?? default;

    /// <summary>
    ///     <para>
    ///         Returns the value of the header with the specified name, casting it to the specified type.
    ///     </para>
    ///     <para>
    ///         It will return the default value for the target type if no header with that name is found in the
    ///         collection.
    ///     </para>
    /// </summary>
    /// <param name="headers">
    ///     The enumerable containing the headers to be searched.
    /// </param>
    /// <param name="name">
    ///     The name of the header to be retrieved.
    /// </param>
    /// <param name="targetType">
    ///     The type to convert the header value to.
    /// </param>
    /// <returns>
    ///     The header value converted to the target type, or <c>null</c> if not found.
    /// </returns>
    public static object? GetValueOrDefault(
        this IEnumerable<MessageHeader> headers,
        string name,
        Type targetType) =>
        GetValue(headers, name, targetType) ?? targetType.GetDefaultValue();
}
