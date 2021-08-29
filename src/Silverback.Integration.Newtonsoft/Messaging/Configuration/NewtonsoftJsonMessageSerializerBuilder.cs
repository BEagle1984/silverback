// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <inheritdoc cref="INewtonsoftJsonMessageSerializerBuilder" />
    public class NewtonsoftJsonMessageSerializerBuilder : INewtonsoftJsonMessageSerializerBuilder
    {
        private NewtonsoftJsonMessageSerializerBase? _serializer;

        private JsonSerializerSettings? _settings;

        private MessageEncoding? _encoding;

        /// <inheritdoc cref="INewtonsoftJsonMessageSerializerBuilder.UseFixedType{TMessage}" />
        public INewtonsoftJsonMessageSerializerBuilder UseFixedType<TMessage>()
        {
            _serializer = new NewtonsoftJsonMessageSerializer<TMessage>();
            return this;
        }

        /// <inheritdoc cref="INewtonsoftJsonMessageSerializerBuilder.UseFixedType(Type)"/>
        public INewtonsoftJsonMessageSerializerBuilder UseFixedType(Type messageType)
        {
            var serializerType = typeof(NewtonsoftJsonMessageSerializer<>).MakeGenericType(messageType);
            _serializer = (NewtonsoftJsonMessageSerializerBase)Activator.CreateInstance(serializerType);
            return this;
        }

        /// <inheritdoc cref="INewtonsoftJsonMessageSerializerBuilder.Configure" />
        public INewtonsoftJsonMessageSerializerBuilder Configure(
            Action<JsonSerializerSettings> configureAction)
        {
            Check.NotNull(configureAction, nameof(configureAction));

            var settings = new JsonSerializerSettings();
            configureAction.Invoke(settings);
            _settings = settings;

            return this;
        }

        /// <inheritdoc cref="INewtonsoftJsonMessageSerializerBuilder.WithEncoding" />
        public INewtonsoftJsonMessageSerializerBuilder WithEncoding(MessageEncoding encoding)
        {
            _encoding = encoding;
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
            _serializer ??= new NewtonsoftJsonMessageSerializer();

            if (_settings != null)
                _serializer.Settings = _settings;

            if (_encoding != null)
                _serializer.Encoding = _encoding.Value;

            return _serializer;
        }
    }
}
