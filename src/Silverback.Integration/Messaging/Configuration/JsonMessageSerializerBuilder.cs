// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonMessageSerializer" />.
/// </summary>
public sealed class JsonMessageSerializerBuilder
{
    private IJsonMessageSerializer? _serializer;

    private JsonSerializerOptions? _options;

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
        _serializer ??= new JsonMessageSerializer();

        if (_options != null)
            _serializer.Options = _options;

        return _serializer;
    }
}
