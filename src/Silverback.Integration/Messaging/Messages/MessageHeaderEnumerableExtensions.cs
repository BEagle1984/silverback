// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Add some helper methods to the <see cref="IEnumerable{T}"/> of <see cref="MessageHeader"/>.
    /// </summary>
    public static class MessageHeaderEnumerableExtensions
    {
        /// <summary>
        ///     Returns a boolean value indicating whether an header with the specified key has already been added
        ///     to the collection.
        /// </summary>
        /// <param name="headers">
        ///     The enumerable containing the headers to be searched.
        /// </param>
        /// <param name="key"> The key to be checked. </param>
        /// <returns>
        ///     A boolean value indicating whether the key was found in the existing headers.
        /// </returns>
        public static bool Contains(this IEnumerable<MessageHeader> headers, string key) =>
            headers.Any(h => h.Key == key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key.
        ///     </para>
        ///     <para>
        ///         It will return <c> null </c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        /// <param name="headers">
        ///     The enumerable containing the headers to be searched.
        /// </param>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="throwIfNotFound">
        ///     A boolean value specifying whether an exception must be thrown if the header with the specified key
        ///     is not found (or the value cannot be converted to the specified type).
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public static string? GetValue(
            this IEnumerable<MessageHeader> headers,
            string key,
            bool throwIfNotFound = false) =>
            (string?)headers.GetValue(key, typeof(string), throwIfNotFound);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified type
        ///         <typeparamref name="T" />.
        ///     </para>
        ///     <para>
        ///         It will return <c> null </c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        /// <typeparam name="T"> The type to convert the header value to. </typeparam>
        /// <param name="headers">
        ///     The enumerable containing the headers to be searched.
        /// </param>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="throwIfNotFound">
        ///     A boolean value specifying whether an exception must be thrown if the header with the specified key
        ///     is not found (or the value cannot be converted to the specified type).
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public static T? GetValue<T>(this IEnumerable<MessageHeader> headers, string key, bool throwIfNotFound = false)
            where T : struct =>
            (T?)headers.GetValue(key, typeof(T), throwIfNotFound);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified type.
        ///     </para>
        ///     <para>
        ///         By default it will return <c> null </c> if no header with that key is found in the collection
        ///         but this behavior can be changed setting the <paramref name="throwIfNotFound" /> parameter to
        ///         <c> true </c>.
        ///     </para>
        /// </summary>
        /// <param name="headers">
        ///     The enumerable containing the headers to be searched.
        /// </param>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="targetType"> The type to convert the header value to. </param>
        /// <param name="throwIfNotFound">
        ///     A boolean value specifying whether an exception must be thrown if the header with the specified key
        ///     is not found (or the value cannot be converted to the specified type).
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        [SuppressMessage("ReSharper", "CA1031", Justification = "Rethrown if desired")]
        public static object? GetValue(
            this IEnumerable<MessageHeader> headers,
            string key,
            Type targetType,
            bool throwIfNotFound = false)
        {
            Check.NotNull(headers, nameof(headers));
            Check.NotEmpty(key, nameof(key));
            Check.NotNull(targetType, nameof(targetType));

            var header = headers.FirstOrDefault(h => h.Key == key);

            if (header == null)
            {
                if (throwIfNotFound)
                    throw new ArgumentOutOfRangeException(nameof(key), $"No '{key}' header was found.");

                return null;
            }

            if (targetType == typeof(string))
                return header.Value;

            try
            {
                return Convert.ChangeType(header.Value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                if (throwIfNotFound)
                {
                    throw new InvalidOperationException(
                        $"The '{key}' header cannot be converted to {targetType.Name}.",
                        ex);
                }

                return null;
            }
        }

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified type
        ///         <typeparamref name="T" />.
        ///     </para>
        ///     <para>
        ///         It will return the default value for the type <typeparamref name="T" /> if no header with that
        ///         key is found in the collection.
        ///     </para>
        /// </summary>
        /// <typeparam name="T"> The type to convert the header value to. </typeparam>
        /// <param name="headers">
        ///     The enumerable containing the headers to be searched.
        /// </param>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public static T GetValueOrDefault<T>(this IEnumerable<MessageHeader> headers, string key)
            where T : struct =>
            GetValue<T>(headers, key) ?? default;

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified type.
        ///     </para>
        ///     <para>
        ///         It will return the default value for the target type if no header with that key is found in the
        ///         collection.
        ///     </para>
        /// </summary>
        /// <param name="headers">
        ///     The enumerable containing the headers to be searched.
        /// </param>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="targetType"> The type to convert the header value to. </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public static object? GetValueOrDefault(this IEnumerable<MessageHeader> headers, string key, Type targetType) =>
            GetValue(headers, key, targetType) ?? targetType.GetDefaultValue();
    }
}
