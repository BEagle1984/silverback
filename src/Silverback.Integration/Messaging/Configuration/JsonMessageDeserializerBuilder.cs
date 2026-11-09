// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    ///     Configures the <see cref="JsonSerializerOptions" />. The action is applied to a copy of the options set by the previous calls
    ///     to <see cref="Configure" /> or <see cref="UseJsonSerializerContext" />, if any.
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

        JsonSerializerOptions options = _options == null ? new JsonSerializerOptions() : new JsonSerializerOptions(_options);
        configureAction.Invoke(options);
        _options = options;

        return this;
    }

    /// <summary>
    ///     Specifies the source-generated <see cref="JsonSerializerContext" /> to be used. The deserializer will use the context's own
    ///     <see cref="JsonSerializerContext.Options" />, replacing the options set by the previous calls to <see cref="Configure" />.
    ///     Call <see cref="Configure" /> afterwards to further customize a copy of these options, for example to add more contexts to the
    ///     <see cref="JsonSerializerOptions.TypeInfoResolverChain" />. The additional contexts will use these same options.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="JsonSerializerContext" />. Pass the generated <c>Default</c> instance to use the options declared via the
    ///     <see cref="JsonSourceGenerationOptionsAttribute" />.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageDeserializerBuilder UseJsonSerializerContext(JsonSerializerContext context)
    {
        Check.NotNull(context, nameof(context));

        _options = context.Options;
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
