// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;
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

        JsonSerializerSettings settings = new();
        configureAction.Invoke(settings);
        _settings = settings;

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
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    public IMessageSerializer Build()
    {
        NewtonsoftJsonMessageSerializer serializer = new();

        if (_settings != null)
            serializer.Settings = _settings;

        if (_encoding != null)
            serializer.Encoding = _encoding.Value;

        return serializer;
    }
}
