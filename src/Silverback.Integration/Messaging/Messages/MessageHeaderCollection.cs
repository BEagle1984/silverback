// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a modifiable collection of message headers.
    /// </summary>
    public class MessageHeaderCollection : List<MessageHeader>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageHeaderCollection" /> class.
        /// </summary>
        /// <param name="headers"> The headers to be added to the collection. </param>
        public MessageHeaderCollection(IEnumerable<MessageHeader>? headers = null)
        {
            if (headers != null)
                AddRange(headers);
        }

        /// <summary>
        ///     Gets or sets the value of the header with the specified key.
        /// </summary>
        /// <param name="key"> The header key. </param>
        public string? this[string key]
        {
            get => GetValue(Check.NotNull(key, nameof(key)), true);
            set => AddOrReplace(key, value);
        }

        /// <summary> Adds a new header. </summary>
        /// <param name="key"> The header key. </param>
        /// <param name="value"> The header value. </param>
        public void Add(string key, object value)
        {
            Check.NotNull(key, nameof(key));
            Check.NotNull(value, nameof(value));

            Add(key, value.ToString());
        }

        /// <summary> Adds a new header. </summary>
        /// <param name="key"> The header key. </param>
        /// <param name="value"> The header value. </param>
        public void Add(string key, string? value)
        {
            Check.NotNull(key, nameof(key));

            Add(new MessageHeader { Key = key, Value = value });
        }

        /// <summary> Removes all headers with the specified key. </summary>
        /// <param name="key"> The header key. </param>
        public void Remove(string key) =>
            RemoveAll(x => x.Key == key);

        /// <summary>
        ///     Adds a new header or replaces the header with the same key.
        /// </summary>
        /// <param name="key"> The header key. </param>
        /// <param name="newValue"> The new header value. </param>
        public void AddOrReplace(string key, object? newValue)
        {
            Check.NotNull(key, nameof(key));

            AddOrReplace(key, newValue?.ToString());
        }

        /// <summary>
        ///     Adds a new header or replaces the header with the same key.
        /// </summary>
        /// <param name="key"> The header key. </param>
        /// <param name="newValue"> The new header value. </param>
        public void AddOrReplace(string key, string? newValue)
        {
            Check.NotNull(key, nameof(key));

            Remove(key);
            Add(key, newValue);
        }

        /// <summary>
        ///     Returns a boolean value indicating whether an header with the specified key has already been added
        ///     to the collection.
        /// </summary>
        /// <param name="key"> The key to be checked. </param>
        /// <returns>
        ///     A boolean value indicating whether the key was found in the existing headers.
        /// </returns>
        public bool Contains(string key) => this.AsEnumerable().Contains(key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key.
        ///     </para>
        ///     <para>
        ///         It will return <c> null </c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="throwIfNotFound">
        ///     A boolean value specifying whether an exception must be thrown if the header with the specified key
        ///     is not found (or the value cannot be converted to the specified type).
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public string? GetValue(string key, bool throwIfNotFound = false) =>
            this.AsEnumerable().GetValue(key, throwIfNotFound);

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
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="throwIfNotFound">
        ///     A boolean value specifying whether an exception must be thrown if the header with the specified key
        ///     is not found (or the value cannot be converted to the specified type).
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public T? GetValue<T>(string key, bool throwIfNotFound = false)
            where T : struct =>
            this.AsEnumerable().GetValue<T>(key, throwIfNotFound);

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
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="targetType"> The type to convert the header value to. </param>
        /// <param name="throwIfNotFound">
        ///     A boolean value specifying whether an exception must be thrown if the header with the specified key
        ///     is not found (or the value cannot be converted to the specified type).
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public object? GetValue(string key, Type targetType, bool throwIfNotFound = false) =>
            this.AsEnumerable().GetValue(key, targetType, throwIfNotFound);

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
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public T GetValueOrDefault<T>(string key)
            where T : struct =>
            this.AsEnumerable().GetValueOrDefault<T>(key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified type.
        ///     </para>
        ///     <para>
        ///         It will return the default value for the target type if no header with that key is found in the
        ///         collection.
        ///     </para>
        /// </summary>
        /// <param name="key"> The key of the header to be retrieved. </param>
        /// <param name="targetType"> The type to convert the header value to. </param>
        /// <returns>
        ///     The header value converted to the target type, or <c> null </c> if not found.
        /// </returns>
        public object? GetValueOrDefault(string key, Type targetType) =>
            this.AsEnumerable().GetValueOrDefault(key, targetType);
    }
}
