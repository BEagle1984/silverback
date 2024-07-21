// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
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
    ///     Configures the <see cref="JsonSerializerOptions" />.
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

        _options = new JsonSerializerOptions();
        configureAction.Invoke(_options);

        return this;
    }

    /// <summary>
    ///     Specifies that the message type header (see <see cref="DefaultMessageHeaders.MessageType"/>) must be set.
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
    ///     Specifies that the message type header (see <see cref="DefaultMessageHeaders.MessageType"/>) must not be set.
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
