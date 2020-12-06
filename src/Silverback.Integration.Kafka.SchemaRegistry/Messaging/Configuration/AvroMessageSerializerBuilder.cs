// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="IAvroMessageSerializerBuilder" />
    public class AvroMessageSerializerBuilder : IAvroMessageSerializerBuilder
    {
        private AvroMessageSerializerBase? _serializer;

        private Action<SchemaRegistryConfig>? _configureSchemaRegistryAction;

        private Action<AvroSerializerConfig>? _configureSerializerAction;

        /// <inheritdoc cref="IAvroMessageSerializerBuilder.UseType{TMessage}" />
        public IAvroMessageSerializerBuilder UseType<TMessage>()
            where TMessage : class
        {
            _serializer = new AvroMessageSerializer<TMessage>();
            return this;
        }

        /// <inheritdoc cref="IAvroMessageSerializerBuilder.Configure" />
        public IAvroMessageSerializerBuilder Configure(
            Action<SchemaRegistryConfig> configureSchemaRegistryAction,
            Action<AvroSerializerConfig>? configureSerializerAction = null)
        {
            _configureSchemaRegistryAction = Check.NotNull(
                configureSchemaRegistryAction,
                nameof(configureSchemaRegistryAction));
            _configureSerializerAction = configureSerializerAction;

            return this;
        }

        /// <summary>
        ///     Builds the <see cref="IMessageSerializer" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMessageSerializer" />.
        /// </returns>
        public IMessageSerializer Build()
        {
            if (_serializer == null)
            {
                throw new InvalidOperationException(
                    "The message type was not specified. Please call UseType<TMessage>.");
            }

            _configureSchemaRegistryAction?.Invoke(_serializer.SchemaRegistryConfig);
            _configureSerializerAction?.Invoke(_serializer.AvroSerializerConfig);

            return _serializer;
        }
    }
}
