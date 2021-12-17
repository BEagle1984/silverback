// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="AvroMessageSerializer{TMessage}" />.
/// </summary>
public class AvroMessageSerializerBuilder
{
    private AvroMessageSerializerBase? _serializer;

    private Action<SchemaRegistryConfig>? _configureSchemaRegistryAction;

    private Action<AvroSerializerConfig>? _configureSerializerAction;

    /// <summary>
    ///     Specifies the message type.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message to serialize or deserialize.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="AvroMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageSerializerBuilder UseType<TMessage>()
        where TMessage : class
    {
        _serializer = new AvroMessageSerializer<TMessage>();
        return this;
    }

    /// <summary>
    ///     Configures the <see cref="SchemaRegistryConfig" /> and the <see cref="AvroSerializerConfig" />.
    /// </summary>
    /// <param name="configureSchemaRegistryAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="SchemaRegistryConfig" /> and configures it.
    /// </param>
    /// <param name="configureSerializerAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="AvroSerializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageSerializerBuilder Configure(
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
            throw new InvalidOperationException("The message type was not specified. Please call UseType<TMessage>.");

        _configureSchemaRegistryAction?.Invoke(_serializer.SchemaRegistryConfig);
        _configureSerializerAction?.Invoke(_serializer.AvroSerializerConfig);

        return _serializer;
    }
}
