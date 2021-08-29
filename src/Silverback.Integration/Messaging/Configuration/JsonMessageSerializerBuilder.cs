// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="IJsonMessageSerializerBuilder"/>
    public class JsonMessageSerializerBuilder : IJsonMessageSerializerBuilder
    {
        private JsonMessageSerializerBase? _serializer;

        private JsonSerializerOptions? _options;

        /// <inheritdoc cref="IJsonMessageSerializerBuilder.UseFixedType{TMessage}"/>
        public IJsonMessageSerializerBuilder UseFixedType<TMessage>()
        {
            _serializer = new JsonMessageSerializer<TMessage>();
            return this;
        }

        /// <inheritdoc cref="IJsonMessageSerializerBuilder.UseFixedType(Type)"/>
        public IJsonMessageSerializerBuilder UseFixedType(Type messageType)
        {
            var serializerType = typeof(JsonMessageSerializer<>).MakeGenericType(messageType);
            _serializer = (JsonMessageSerializerBase)Activator.CreateInstance(serializerType);
            return this;
        }

        /// <inheritdoc cref="IJsonMessageSerializerBuilder.WithOptions"/>
        public IJsonMessageSerializerBuilder WithOptions(JsonSerializerOptions options)
        {
            _options = options;
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
            _serializer ??= new JsonMessageSerializer();

            if (_options != null)
                _serializer.Options = _options;

            return _serializer;
        }
    }
}
