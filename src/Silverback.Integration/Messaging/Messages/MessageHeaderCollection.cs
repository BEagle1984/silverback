// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a modifiable collection of message headers.
    /// </summary>
    public class MessageHeaderCollection : List<MessageHeader>
    {
        /// <summary>
        ///     Create a new <see cref="MessageHeaderCollection" /> optionally initializing it
        ///     with the specified headers.
        /// </summary>
        /// <param name="headers">The headers to be added to the new <see cref="MessageHeaderCollection" />.</param>
        public MessageHeaderCollection(IEnumerable<MessageHeader> headers = null)
        {
            if (headers != null)
                AddRange(headers);
        }

        /// <summary>
        ///     Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The header key.</param>
        public string this[string key]
        {
            get => GetValue(key) ??
                   throw new ArgumentOutOfRangeException(nameof(key));
            set => AddOrReplace(key, value);
        }

        /// <summary>
        ///     Adds a new header.
        /// </summary>
        public void Add(string key, object value) =>
            Add(key, value.ToString());

        /// <summary>
        ///     Adds a new header.
        /// </summary>
        public void Add(string key, string value) =>
            Add(new MessageHeader { Key = key, Value = value });

        /// <summary>
        ///     Removes all headers matching the specified key.
        /// </summary>
        public void Remove(string key) =>
            RemoveAll(x => x.Key == key);

        /// <summary>
        ///     Adds a new header or replaces the header with the same key.
        /// </summary>
        public void AddOrReplace(string key, object newValue) =>
            AddOrReplace(key, newValue.ToString());

        /// <summary>
        ///     Adds a new header or replaces the header with the same key.
        /// </summary>
        public void AddOrReplace(string key, string newValue)
        {
            Remove(key);
            Add(key, newValue);
        }

        /// <summary>
        ///     Returns a boolean value indicating whether an header with the specified key
        ///     has already been added to the collection.
        /// </summary>
        public bool Contains(string key) => this.AsEnumerable().Contains(key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        public string GetValue(string key) => this.AsEnumerable().GetValue(key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified
        ///         type <typeparamref name="T" />.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        public T? GetValue<T>(string key)
            where T : struct =>
            this.AsEnumerable().GetValue<T>(key);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified key, casting it to the specified
        ///         type.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that key is found in the collection.
        ///     </para>
        /// </summary>
        public object GetValue(string key, Type targetType) => this.AsEnumerable().GetValue(key, targetType);

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
        public T GetValueOrDefault<T>(string key)
            where T : struct =>
            this.AsEnumerable().GetValueOrDefault<T>(key);

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
        public object GetValueOrDefault(string key, Type targetType) =>
            this.AsEnumerable().GetValueOrDefault(key, targetType);
    }
}