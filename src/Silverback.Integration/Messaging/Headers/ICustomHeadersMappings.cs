// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Headers
{
    /// <summary>
    ///     Can be used to override the default header names.
    /// </summary>
    public interface ICustomHeadersMappings
    {
        /// <summary>
        ///     Gets the number of mappings that have been configured.
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Adds a new mapping.
        /// </summary>
        /// <param name="defaultHeaderName">
        ///     The default header name.
        /// </param>
        /// <param name="customHeaderName">
        ///     The custom header name to be used instead of the default.
        /// </param>
        public void Add(string defaultHeaderName, string customHeaderName);

        /// <summary>
        ///     Applies the configured mappings to the specified collection of <see cref="MessageHeader" />.
        /// </summary>
        /// <param name="headers">
        ///     The collection of <see cref="MessageHeader" /> to be mapped.
        /// </param>
        public void Apply(IEnumerable<MessageHeader> headers);

        /// <summary>
        ///     Reverts the headers in the specified collection of <see cref="MessageHeader" /> to the default header
        ///     names.
        /// </summary>
        /// <param name="headers">
        ///     The collection of <see cref="MessageHeader" /> to be mapped.
        /// </param>
        public void Revert(IEnumerable<MessageHeader> headers);
    }
}
