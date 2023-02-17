// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="AvroMessageDeserializer{TMessage}" />.
/// </summary>
public class AvroMessageDeserializerBuilder
{
    private IAvroMessageDeserializer? _deserializer;

    private Action<SchemaRegistryConfig>? _configureSchemaRegistryAction;

    private Action<AvroDeserializerConfig>? _configureDeserializerAction;

    /// <summary>
    ///     Specifies the message type.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="AvroMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageDeserializerBuilder UseModel<TMessage>()
        where TMessage : class
    {
        _deserializer = new AvroMessageDeserializer<TMessage>();
        return this;
    }

    /// <summary>
    ///     Specifies the message type.
    /// </summary>
    /// <param name="messageType">
    ///     The type of the message to serialize or deserialize.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageDeserializerBuilder UseModel(Type messageType)
    {
        Type deserializerType = typeof(AvroMessageDeserializer<>).MakeGenericType(messageType);
        _deserializer = (IAvroMessageDeserializer)Activator.CreateInstance(deserializerType)!;
        return this;
    }

    /// <summary>
    ///     Configures the <see cref="SchemaRegistryConfig" /> and the <see cref="AvroSerializerConfig" />.
    /// </summary>
    /// <param name="configureSchemaRegistryAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="SchemaRegistryConfig" /> and configures it.
    /// </param>
    /// <param name="configureDeserializerAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="AvroDeserializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageDeserializerBuilder Configure(
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
    ///     Builds the <see cref="IMessageDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageDeserializer" />.
    /// </returns>
    public IMessageDeserializer Build()
    {
        if (_deserializer == null)
        {
            throw new InvalidOperationException("The message type was not specified. Please call UseModel<TMessage>.");
        }

        _configureSchemaRegistryAction?.Invoke(_deserializer.SchemaRegistryConfig);
        _configureDeserializerAction?.Invoke(_deserializer.AvroDeserializerConfig);

        return _deserializer;
    }
}
