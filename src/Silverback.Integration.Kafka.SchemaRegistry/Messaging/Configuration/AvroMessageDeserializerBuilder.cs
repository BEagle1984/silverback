// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="IAvroMessageDeserializerBuilder" />
    public class AvroMessageDeserializerBuilder : IAvroMessageDeserializerBuilder
    {
        private AvroMessageDeserializerBase? _deserializer;

        private Action<SchemaRegistryConfig>? _configureSchemaRegistryAction;

        private Action<AvroDeserializerConfig>? _configureDeserializerAction;

        /// <inheritdoc cref="IAvroMessageDeserializerBuilder.UseType" />
        public IAvroMessageDeserializerBuilder UseType(Type messageType)
        {
            var deserializerType = typeof(AvroMessageDeserializer<>).MakeGenericType(messageType);
            _deserializer = (AvroMessageDeserializerBase)Activator.CreateInstance(deserializerType);
            return this;
        }

        /// <inheritdoc cref="IAvroMessageDeserializerBuilder.UseType{TMessage}" />
        public IAvroMessageDeserializerBuilder UseType<TMessage>()
            where TMessage : class
        {
            _deserializer = new AvroMessageDeserializer<TMessage>();
            return this;
        }

        /// <inheritdoc cref="IAvroMessageDeserializerBuilder.Configure" />
        public IAvroMessageDeserializerBuilder Configure(
            Action<SchemaRegistryConfig> configureSchemaRegistryAction,
            Action<AvroDeserializerConfig>? configureDeserializerAction = null)
        {
            _configureSchemaRegistryAction = Check.NotNull(
                configureSchemaRegistryAction,
                nameof(configureSchemaRegistryAction));
            _configureDeserializerAction = configureDeserializerAction;

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
            if (_deserializer == null)
            {
                throw new InvalidOperationException("The message type was not specified. Please call UseType<TMessage>.");
            }

            _configureSchemaRegistryAction?.Invoke(_deserializer.SchemaRegistryConfig);
            _configureDeserializerAction?.Invoke(_deserializer.AvroDeserializerConfig);

            return _deserializer;
        }
    }
}
