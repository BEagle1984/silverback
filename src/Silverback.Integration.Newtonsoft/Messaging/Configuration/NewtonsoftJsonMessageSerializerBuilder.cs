// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="NewtonsoftJsonMessageSerializer" />.
/// </summary>
public class NewtonsoftJsonMessageSerializerBuilder
{
    private JsonSerializerSettings? _settings;

    private MessageEncoding? _encoding;

    private bool? _mustSetTypeHeader;

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
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder WithEncoding(MessageEncoding encoding)
    {
        _encoding = encoding;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header (see <see cref="DefaultMessageHeaders.MessageType"/>) must be set.
    ///     This is necessary when sending multiple message type through the same endpoint, to allow Silverback to automatically figure out
    ///     the correct type to deserialize into.
    /// </summary>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder SetTypeHeader()
    {
        _mustSetTypeHeader = true;
        return this;
    }

    /// <summary>
    ///     Specifies that the message type header (see <see cref="DefaultMessageHeaders.MessageType"/>) must not be set.
    /// </summary>
    /// <returns>
    ///     The <see cref="NewtonsoftJsonMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public NewtonsoftJsonMessageSerializerBuilder DisableTypeHeader()
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
    public IMessageSerializer Build() => new NewtonsoftJsonMessageSerializer(_settings, _encoding, _mustSetTypeHeader);
}
