// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="NewtonsoftJsonMessageSerializer{TMessage}" /> or <see cref="NewtonsoftJsonMessageSerializer{TMessage}" />.
/// </summary>
public class NewtonsoftJsonMessageSerializerBuilder
{
    private INewtonsoftJsonMessageSerializer? _serializer;

    private JsonSerializerSettings? _settings;

    private MessageEncoding? _encoding;

    /// <summary>
    ///     Specifies a fixed message type. This will prevent the message type header to be written when
    ///     serializing and the header will be ignored when deserializing.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize or deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder UseFixedType<TMessage>()
    {
        _serializer = new NewtonsoftJsonMessageSerializer<TMessage>();
        return this;
    }

    /// <summary>
    ///     Specifies a fixed message type. This will prevent the message type header to be written when
    ///     serializing and the header will be ignored when deserializing.
    /// </summary>
    /// <param name="messageType">
    ///     The type of the message to serialize or deserialize.
    /// </param>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder UseFixedType(Type messageType)
    {
        Type serializerType = typeof(NewtonsoftJsonMessageSerializer<>).MakeGenericType(messageType);
        _serializer = (INewtonsoftJsonMessageSerializer)Activator.CreateInstance(serializerType)!;
        return this;
    }

    /// <summary>
    ///     Configures the <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSerializerSettings" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder Configure(Action<JsonSerializerSettings> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        JsonSerializerSettings settings = new();
        configureAction.Invoke(settings);
        _settings = settings;

        return this;
    }

    /// <summary>
    ///     Specifies the encoding to be used.
    /// </summary>
    /// <param name="encoding">
    ///     The <see cref="MessageEncoding" />.
    /// </param>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder WithEncoding(MessageEncoding encoding)
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
        _serializer ??= new NewtonsoftJsonMessageSerializer<object>();

        if (_settings != null)
            _serializer.Settings = _settings;

        if (_encoding != null)
            _serializer.Encoding = _encoding.Value;

        return _serializer;
    }
}
