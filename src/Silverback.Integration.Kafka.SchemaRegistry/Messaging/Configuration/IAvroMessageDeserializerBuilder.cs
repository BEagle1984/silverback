// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="AvroMessageDeserializer{TMessage}" />.
    /// </summary>
    public interface IAvroMessageDeserializerBuilder
    {
        /// <summary>
        ///     Specifies the message type.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the message to serialize or deserialize.
        /// </param>
        /// <returns>
        ///     The <see cref="IAvroMessageDeserializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IAvroMessageDeserializerBuilder UseType(Type messageType);

        /// <summary>
        ///     Specifies the message type.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the message to serialize or deserialize.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IAvroMessageDeserializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IAvroMessageDeserializerBuilder UseType<TMessage>()
            where TMessage : class;

        /// <summary>
        ///     Configures the <see cref="SchemaRegistryConfig" /> and the <see cref="AvroDeserializerConfig" />.
        /// </summary>
        /// <param name="configureSchemaRegistryAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="SchemaRegistryConfig" /> and configures it.
        /// </param>
        /// <param name="configureDeserializerAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="AvroDeserializerConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IAvroMessageDeserializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IAvroMessageDeserializerBuilder Configure(
            Action<SchemaRegistryConfig> configureSchemaRegistryAction,
            Action<AvroDeserializerConfig>? configureDeserializerAction = null);
    }
}
