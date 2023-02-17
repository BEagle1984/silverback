// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonMessageDeserializer{TMessage}" />.
/// </summary>
public sealed class JsonMessageDeserializerBuilder
{
    private IJsonMessageDeserializer? _deserializer;

    private JsonSerializerOptions? _options;

    /// <summary>
    ///     Specifies the message type. The deserialization will work regardless of the message type header (ideal for interoperability) and
    ///     by default the message type header will be omitted by the producer (unless a sub-type is being produced is called).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize or deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder UseModel<TMessage>()
    {
        _deserializer = new JsonMessageDeserializer<TMessage>();
        return this;
    }

    /// <summary>
    ///     Specifies the message type. The deserialization will work regardless of the message type header (ideal for interoperability) and
    ///     by default the message type header will be omitted by the producer (unless a sub-type is being produced is called).
    /// </summary>
    /// <param name="messageType">
    ///     The type of the message to serialize or deserialize.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder UseModel(Type messageType)
    {
        Type deserializerType = typeof(JsonMessageDeserializer<>).MakeGenericType(messageType);
        _deserializer = (IJsonMessageDeserializer)Activator.CreateInstance(deserializerType)!;
        return this;
    }

    /// <summary>
    ///     Specifies the <see cref="JsonSerializerOptions" />.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="JsonSerializerOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder WithOptions(JsonSerializerOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageDeserializer" />.
    /// </returns>
    public IMessageDeserializer Build()
    {
        _deserializer ??= new JsonMessageDeserializer<object>();

        if (_options != null)
            _deserializer.Options = _options;

        return _deserializer;
    }
}
