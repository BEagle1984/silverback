// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonMessageSerializer" />.
/// </summary>
public sealed class JsonMessageSerializerBuilder
{
    private JsonSerializerOptions? _options;

    private bool? _mustSetTypeHeader;

    /// <summary>
    ///     Configures the <see cref="JsonSerializerOptions" />. The action is applied to a copy of the options set by the previous calls
    ///     to <see cref="Configure" /> or <see cref="UseJsonSerializerContext" />, if any.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSerializerOptions" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder Configure(Action<JsonSerializerOptions> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        JsonSerializerOptions options = _options == null ? new JsonSerializerOptions() : new JsonSerializerOptions(_options);
        configureAction.Invoke(options);
        _options = options;

        return this;
    }

    /// <summary>
    ///     Specifies the source-generated <see cref="JsonSerializerContext" /> to be used. The serializer will use the context's own
    ///     <see cref="JsonSerializerContext.Options" />, replacing the options set by the previous calls to <see cref="Configure" />.
    ///     Call <see cref="Configure" /> afterwards to further customize a copy of these options, for example to add more contexts to the
    ///     <see cref="JsonSerializerOptions.TypeInfoResolverChain" />. The additional contexts will use these same options.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="JsonSerializerContext" />. Pass the generated <c>Default</c> instance to use the options declared via the
    ///     <see cref="JsonSourceGenerationOptionsAttribute" />.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder UseJsonSerializerContext(JsonSerializerContext context)
    {
        Check.NotNull(context, nameof(context));

        _options = context.Options;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header (see <see cref="DefaultMessageHeaders.MessageType" />) must be set.
    ///     This is necessary when sending multiple message type through the same endpoint, to allow Silverback to automatically figure out
    ///     the correct type to deserialize into.
    /// </summary>
    /// <returns>
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder SetTypeHeader()
    {
        _mustSetTypeHeader = true;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header (see <see cref="DefaultMessageHeaders.MessageType" />) must not be set.
    /// </summary>
    /// <returns>
    ///     The <see cref="JsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonMessageSerializerBuilder DisableTypeHeader()
    {
        _mustSetTypeHeader = false;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    public IMessageSerializer Build() => new JsonMessageSerializer(_options, _mustSetTypeHeader);
}
