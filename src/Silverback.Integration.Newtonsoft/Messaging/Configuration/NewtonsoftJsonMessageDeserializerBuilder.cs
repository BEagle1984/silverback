// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="NewtonsoftJsonMessageDeserializer{TMessage}" /> or <see cref="NewtonsoftJsonMessageDeserializer{TMessage}" />.
/// </summary>
public class NewtonsoftJsonMessageDeserializerBuilder
{
    private Type _messageType = typeof(object);

    private JsonSerializerSettings? _settings;

    private MessageEncoding? _encoding;

    private JsonMessageDeserializerTypeHeaderBehavior? _typeHeaderBehavior;

    /// <summary>
    ///     Specifies a fixed message type. This will prevent the message type header to be written when
    ///     serializing and the header will be ignored when deserializing.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize or deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder UseModel<TMessage>()
    {
        _messageType = typeof(TMessage);
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
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder UseModel(Type messageType)
    {
        _messageType = Check.NotNull(messageType, nameof(messageType));
        return this;
    }

    /// <summary>
    ///     Configures the <see cref="JsonSerializerSettings" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSerializerSettings" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder Configure(Action<JsonSerializerSettings> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _settings = new JsonSerializerSettings();
        configureAction.Invoke(_settings);

        return this;
    }

    /// <summary>
    ///     Specifies the encoding to be used.
    /// </summary>
    /// <param name="encoding">
    ///     The <see cref="MessageEncoding" />.
    /// </param>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder WithEncoding(MessageEncoding encoding)
    {
        _encoding = encoding;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header must be used when sent with the consumed message, otherwise the predefined model has to be used.
    /// </summary>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder WithOptionalMessageTypeHeader()
    {
        _typeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Optional;
        return this;
    }

    /// <summary>
    ///     Specifies that an exception must be thrown if the consumed message doesn't specify the message type header.
    /// </summary>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder WithMandatoryMessageTypeHeader()
    {
        _typeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Mandatory;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header must be ignored. The message will always be deserialized into the predefined model.
    /// </summary>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageDeserializerBuilder IgnoreMessageTypeHeader()
    {
        _typeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Ignore;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageDeserializer" />.
    /// </returns>
    public IMessageDeserializer Build() =>
        (IMessageDeserializer?)Activator.CreateInstance(
            typeof(NewtonsoftJsonMessageDeserializer<>).MakeGenericType(_messageType),
            _settings,
            _encoding,
            _typeHeaderBehavior)
        ?? throw new InvalidOperationException("Failed to create the NewtonsoftJsonMessageDeserializer instance.");
}
