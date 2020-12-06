// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="AvroMessageSerializer{TMessage}" />.
    /// </summary>
    public interface IAvroMessageSerializerBuilder
    {
        /// <summary>
        ///     Specifies the message type.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the message to serialize or deserialize.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IAvroMessageSerializerBuilder UseType<TMessage>()
            where TMessage : class;

        /// <summary>
        ///     Configures the <see cref="SchemaRegistryConfig" /> and the <see cref="AvroSerializerConfig" />.
        /// </summary>
        /// <param name="configureSchemaRegistryAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="SchemaRegistryConfig" /> and configures it.
        /// </param>
        /// <param name="configureSerializerAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="AvroSerializerConfig" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
        /// </returns>
        IAvroMessageSerializerBuilder Configure(
            Action<SchemaRegistryConfig> configureSchemaRegistryAction,
            Action<AvroSerializerConfig>? configureSerializerAction = null);
    }
}
