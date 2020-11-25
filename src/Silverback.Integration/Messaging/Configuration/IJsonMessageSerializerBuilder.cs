// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="JsonMessageSerializer" /> or <see cref="JsonMessageSerializer{TMessage}" />.
    /// </summary>
    public interface IJsonMessageSerializerBuilder
    {
        /// <summary>
        ///     Specifies a fixed message type. This will prevent the message type header to be written when
        ///     serializing and the header will be ignored when deserializing.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the message to serialize or deserialize.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IJsonMessageSerializerBuilder UseFixedType<TMessage>();

        /// <summary>
        ///     Specifies the <see cref="JsonSerializerOptions" />.
        /// </summary>
        /// <param name="options">
        ///     The <see cref="JsonSerializerOptions" />.
        /// </param>
        /// <returns>
        ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IJsonMessageSerializerBuilder WithOptions(JsonSerializerOptions options);
    }
}
