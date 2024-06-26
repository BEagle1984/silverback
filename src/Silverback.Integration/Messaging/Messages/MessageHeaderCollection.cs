﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     A modifiable collection of message headers.
    /// </summary>
    public class MessageHeaderCollection : IReadOnlyList<MessageHeader>
    {
        private List<MessageHeader> _list;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageHeaderCollection" /> class.
        /// </summary>
        /// <param name="capacity">
        ///     The initial capacity of the backing <see cref="List{T}" />.
        /// </param>
        public MessageHeaderCollection(int capacity = 0)
        {
            _list = new(capacity);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageHeaderCollection" /> class.
        /// </summary>
        /// <param name="headers">
        ///     The headers to be added to the collection.
        /// </param>
        public MessageHeaderCollection(IReadOnlyCollection<MessageHeader>? headers)
        {
            _list = headers != null ? new List<MessageHeader>(headers) : new();
        }

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
        public int Count => _list.Count;

        /// <summary>
        ///     Gets or sets the value of the header with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        public string? this[string name]
        {
            get => GetValue(Check.NotNull(name, nameof(name)), true);
            set => AddOrReplace(name, value);
        }

        /// <summary>
        ///     Gets the header at the specified index in the collection.
        /// </summary>
        /// <param name="index">
        ///     The index in the collection.
        /// </param>
        public MessageHeader this[int index] => _list[index];

        /// <summary>
        ///     Adds a new header.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="value">
        ///     The header value.
        /// </param>
        public void Add(string name, object value)
        {
            Check.NotNull(name, nameof(name));
            Check.NotNull(value, nameof(value));

            Add(name, value.ToString());
        }

        /// <summary>
        ///     Adds a new header.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="value">
        ///     The header value.
        /// </param>
        public void Add(string name, string? value)
        {
            Check.NotNull(name, nameof(name));

            Add(new MessageHeader(name, value));
        }

        /// <summary>
        ///     Adds a new header.
        /// </summary>
        /// <param name="header">
        ///     The header to be added.
        /// </param>
        public void Add(MessageHeader header)
        {
            Check.NotNull(header, nameof(header));

            var newList = new List<MessageHeader>(_list.Capacity + 1);
            newList.AddRange(_list);
            newList.Add(header);
            _list = newList;
        }

        /// <summary>
        ///     Removes all headers with the specified name.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        public void Remove(string name)
        {
            var newList = new List<MessageHeader>(_list);
            newList.RemoveAll(x => x.Name == name);
            _list = newList;
        }

        /// <summary>
        ///     Removes the specified header.
        /// </summary>
        /// <param name="header">
        ///     The header to remove.
        /// </param>
        public void Remove(MessageHeader header)
        {
            var newList = new List<MessageHeader>(_list);
            newList.Remove(header);
            _list = newList;
        }

        /// <summary>
        ///     Adds a new header or replaces the header with the same name.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="newValue">
        ///     The new header value.
        /// </param>
        public void AddOrReplace(string name, object? newValue)
        {
            Check.NotNull(name, nameof(name));

            AddOrReplace(name, newValue?.ToString());
        }

        /// <summary>
        ///     Adds a new header or replaces the header with the same name.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="newValue">
        ///     The new header value.
        /// </param>
        public void AddOrReplace(string name, string? newValue)
        {
            Check.NotNull(name, nameof(name));

            Remove(name);
            Add(name, newValue);
        }

        /// <summary>
        ///     Adds a new header if no header with the same name is already set.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="newValue">
        ///     The new header value.
        /// </param>
        public void AddIfNotExists(string name, string? newValue)
        {
            Check.NotNull(name, nameof(name));

            if (!Contains(name))
                Add(name, newValue);
        }

        /// <summary>
        ///     Returns a boolean value indicating whether an header with the specified name has already been added
        ///     to the collection.
        /// </summary>
        /// <param name="name">
        ///     The name to be checked.
        /// </param>
        /// <returns>
        ///     A boolean value indicating whether the name was found in the existing headers.
        /// </returns>
        public bool Contains(string name) => this.AsEnumerable().Contains(name);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified name.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that name is found in the collection.
        ///     </para>
        /// </summary>
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
        public string? GetValue(string name, bool throwIfNotFound = false) =>
            this.AsEnumerable().GetValue(name, throwIfNotFound);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified name, casting it to the specified type
        ///         <typeparamref name="T" />.
        ///     </para>
        ///     <para>
        ///         It will return <c>null</c> if no header with that name is found in the collection.
        ///     </para>
        /// </summary>
        /// <typeparam name="T">
        ///     The type to convert the header value to.
        /// </typeparam>
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
        public T? GetValue<T>(string name, bool throwIfNotFound = false)
            where T : struct =>
            this.AsEnumerable().GetValue<T>(name, throwIfNotFound);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified name, casting it to the specified type.
        ///     </para>
        ///     <para>
        ///         By default it will return <c>null</c> if no header with that name is found in the collection but
        ///         this behavior can be changed
        ///         setting the <paramref name="throwIfNotFound" /> parameter to <c>true</c>.
        ///     </para>
        /// </summary>
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
        public object? GetValue(string name, Type targetType, bool throwIfNotFound = false) =>
            this.AsEnumerable().GetValue(name, targetType, throwIfNotFound);

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
        /// <param name="name">
        ///     The name of the header to be retrieved.
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c>null</c> if not found.
        /// </returns>
        public T GetValueOrDefault<T>(string name)
            where T : struct =>
            this.AsEnumerable().GetValueOrDefault<T>(name);

        /// <summary>
        ///     <para>
        ///         Returns the value of the header with the specified name, casting it to the specified type.
        ///     </para>
        ///     <para>
        ///         It will return the default value for the target type if no header with that name is found in the
        ///         collection.
        ///     </para>
        /// </summary>
        /// <param name="name">
        ///     The name of the header to be retrieved.
        /// </param>
        /// <param name="targetType">
        ///     The type to convert the header value to.
        /// </param>
        /// <returns>
        ///     The header value converted to the target type, or <c>null</c> if not found.
        /// </returns>
        public object? GetValueOrDefault(string name, Type targetType) =>
            this.AsEnumerable().GetValueOrDefault(name, targetType);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<MessageHeader> GetEnumerator() => _list.GetEnumerator();

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }
}
