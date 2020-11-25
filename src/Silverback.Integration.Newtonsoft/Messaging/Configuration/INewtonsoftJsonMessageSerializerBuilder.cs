// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="NewtonsoftJsonMessageSerializer" /> or <see cref="NewtonsoftJsonMessageSerializer{TMessage}" />.
    /// </summary>
    public interface INewtonsoftJsonMessageSerializerBuilder
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
        INewtonsoftJsonMessageSerializerBuilder UseFixedType<TMessage>();

        /// <summary>
        ///     Specifies the <see cref="JsonSerializerSettings" />.
        /// </summary>
        /// <param name="settings">
        ///     The <see cref="JsonSerializerSettings" />.
        /// </param>
        /// <returns>
        ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        INewtonsoftJsonMessageSerializerBuilder WithSettings(JsonSerializerSettings settings);

        /// <summary>
        ///     Specifies the encoding to be used.
        /// </summary>
        /// <param name="encoding">
        ///     The <see cref="MessageEncoding" />.
        /// </param>
        /// <returns>
        ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        INewtonsoftJsonMessageSerializerBuilder WithEncoding(MessageEncoding encoding);
    }
}
