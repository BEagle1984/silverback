// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonMessageSerializer{TMessage}" />.
/// </summary>
public sealed class JsonMessageSerializerBuilder
    {
    private IJsonMessageSerializer? _serializer;

    private JsonSerializerOptions? _options;

    /// <summary>
    ///     Specifies the message type. The deserialization will work regardless of the message type header (ideal for interoperability) and
    ///     the message type header will be omitted by the producer (unless a sub-type is being produced).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize or deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder UseFixedType<TMessage>()
    {
        _serializer = new JsonMessageSerializer<TMessage>();
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
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder UseFixedType(Type messageType)
    {
        Type serializerType = typeof(JsonMessageSerializer<>).MakeGenericType(messageType);
        _serializer = (IJsonMessageSerializer)Activator.CreateInstance(serializerType);
        return this;
    }

    /// <summary>
    ///     Specifies the <see cref="JsonSerializerOptions" />.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="JsonSerializerOptions" />.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder WithOptions(JsonSerializerOptions options)
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
        _serializer ??= new JsonMessageSerializer<object>();

        if (_options != null)
            _serializer.Options = _options;

        return _serializer;
    }
}
