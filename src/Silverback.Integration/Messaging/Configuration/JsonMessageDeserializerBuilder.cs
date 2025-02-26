// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonMessageDeserializer{TMessage}" />.
/// </summary>
public sealed class JsonMessageDeserializerBuilder
{
    private Type _messageType = typeof(object);

    private JsonSerializerOptions? _options;

    private JsonMessageDeserializerTypeHeaderBehavior? _typeHeaderBehavior;

    /// <summary>
    ///     Specifies the message type. The deserialization will work regardless of the message type header (ideal for interoperability) and
    ///     by default the message type header will be omitted by the producer (unless a subtype is being produced is called).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize or deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder UseModel<TMessage>()
    {
        _messageType = typeof(TMessage);
        return this;
    }

    /// <summary>
    ///     Specifies the message type. The deserialization will work regardless of the message type header (ideal for interoperability) and
    ///     by default the message type header will be omitted by the producer (unless a subtype is being produced is called).
    /// </summary>
    /// <param name="messageType">
    ///     The type of the message to serialize or deserialize.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder UseModel(Type messageType)
    {
        _messageType = Check.NotNull(messageType, nameof(messageType));
        return this;
    }

    /// <summary>
    ///     Configures the <see cref="JsonSerializerOptions" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSerializerOptions" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder Configure(Action<JsonSerializerOptions> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _options = new JsonSerializerOptions();
        configureAction.Invoke(_options);

        return this;
    }

    /// <summary>
    ///     Specifies that the message type header must be used when sent with the consumed message, otherwise the predefined model has to be used.
    /// </summary>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder WithOptionalMessageTypeHeader()
    {
        _typeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Optional;
        return this;
    }

    /// <summary>
    ///     Specifies that an exception must be thrown if the consumed message doesn't specify the message type header.
    /// </summary>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder WithMandatoryMessageTypeHeader()
    {
        _typeHeaderBehavior = JsonMessageDeserializerTypeHeaderBehavior.Mandatory;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header must be ignored. The message will always be deserialized into the predefined model.
    /// </summary>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder IgnoreMessageTypeHeader()
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
            typeof(JsonMessageDeserializer<>).MakeGenericType(_messageType),
            _options,
            _typeHeaderBehavior) ??
        throw new InvalidOperationException("Failed to create the JsonMessageDeserializer instance.");
}
