// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    public static class MessageHeaderCollectionExtensions
    {
        /// <summary>
        ///     Returns a boolean value indicating whether an header with the specified key
        ///     has already been added to the collection.
        /// </summary>
        public static bool Contains(this IEnumerable<MessageHeader> headers, string key) =>
            headers.Any(h => h.Key == key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        public static string GetValue(this IEnumerable<MessageHeader> headers, string key) =>
            headers.FirstOrDefault(h => h.Key == key)?.Value;

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified
        ///         type <typeparamref name="T" />.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        public static T? GetValue<T>(this IEnumerable<MessageHeader> headers, string key)
            where T : struct =>
            (T?) headers.GetValue(key, typeof(T));

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified
        ///         type.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        public static object GetValue(this IEnumerable<MessageHeader> headers, string key, Type targetType)
        {
            var value = headers.FirstOrDefault(h => h.Key == key)?.Value;

            if (value == null)
                return null;

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified
        ///         type <typeparamref name="T" />.
        ///     </para>
        ///     <para>
        ///         It will return the default value for the type <typeparamref name="T" /> if no header
        ///         with that key is found in the collection.
        ///     </para>
        /// </summary>
        public static T GetValueOrDefault<T>(this IEnumerable<MessageHeader> headers, string key)
            where T : struct =>
            GetValue<T>(headers, key) ?? default;

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified
        ///         type.
        ///     </para>
        ///     <para>
        ///         It will return the default value for the target type if no header
        ///         with that key is found in the collection.
        ///     </para>
        /// </summary>
        public static object GetValueOrDefault(this IEnumerable<MessageHeader> headers, string key, Type targetType) =>
            GetValue(headers, key, targetType) ?? targetType.GetDefaultValue();
    }
}